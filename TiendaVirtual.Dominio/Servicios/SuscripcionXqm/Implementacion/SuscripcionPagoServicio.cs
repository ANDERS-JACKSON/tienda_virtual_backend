using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Extensiones.PagoXqm;
using TiendaVirtual.Dominio.Modelo.PagoXqm;
using TiendaVirtual.Dominio.Modelo.SoporteXqm;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.PagoXqm;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;

namespace TiendaVirtual.Dominio.Servicios.SuscripcionXqm.Implementacion
{
    public class SuscripcionPagoServicio : ISuscripcionPagoServicio
    {
        private readonly TiendaVirtualDbContext _context;
        private readonly IConfiguration _config;

        public SuscripcionPagoServicio(TiendaVirtualDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<ResultadoOperacion<RespuestaInicioPagoDto>> IniciarPagoAsync(
            int usuarioId, IniciarPagoSuscripcionDto dto)
        {
            await using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                var vendedor = await _context.Vendedores.FirstOrDefaultAsync(v => v.UsuarioId == usuarioId);
                if (vendedor == null)
                    return ResultadoOperacion<RespuestaInicioPagoDto>.SetError("No tienes perfil de vendedor.");

                var sus = await _context.Suscripciones
                    .Include(s => s.Plan)
                    .Include(s => s.Cupon)
                    .FirstOrDefaultAsync(s =>
                        s.SuscripcionId == dto.SuscripcionId && s.VendedorId == vendedor.VendedorId);

                if (sus == null)
                    return ResultadoOperacion<RespuestaInicioPagoDto>.SetError("Suscripción no encontrada.");

                if (sus.Estado != TipoEstadoSuscripcion.PendientePago)
                    return ResultadoOperacion<RespuestaInicioPagoDto>.SetError(
                        "Solo se puede iniciar el pago si la suscripción está en estado PendientePago.");

                var monto = CalcularMontoConDescuento(sus);

                var transaccionExistente = await _context.Transacciones
                    .Where(t => t.SuscripcionId == sus.SuscripcionId &&
                                (t.Estado == TipoEstadoTransaccion.Pendiente ||
                                 t.Estado == TipoEstadoTransaccion.Procesando))
                    .OrderByDescending(t => t.TransaccionId)
                    .FirstOrDefaultAsync();

                Transaccion transaccion;
                if (transaccionExistente != null && transaccionExistente.Monto == monto)
                {
                    transaccion = transaccionExistente;
                }
                else
                {
                    if (transaccionExistente != null)
                        transaccionExistente.Estado = TipoEstadoTransaccion.Cancelada;

                    transaccion = new Transaccion
                    {
                        SuscripcionId = sus.SuscripcionId,
                        UsuarioId = usuarioId,
                        Proveedor = "IZIPAY",
                        Tipo = TipoTransaccion.PagoSuscripcion,
                        Monto = monto,
                        Estado = TipoEstadoTransaccion.Pendiente,
                        Fecha = DateTime.UtcNow
                    };
                    _context.Transacciones.Add(transaccion);
                    await _context.SaveChangesAsync();
                }

                var (formToken, publicKey) = await GenerarFormTokenIzipayAsync(transaccion, sus);
                await trx.CommitAsync();

                return ResultadoOperacion<RespuestaInicioPagoDto>.SetExito(new RespuestaInicioPagoDto
                {
                    TransaccionId = transaccion.TransaccionId,
                    Monto = transaccion.Monto,
                    Moneda = "PEN",
                    Concepto = $"Suscripción {sus.Plan.Nombre} - {sus.Plan.Codigo}",
                    FormToken = formToken,
                    PublicKey = publicKey
                });
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                return ResultadoOperacion<RespuestaInicioPagoDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<TransaccionDto>> ConfirmarPagoAsync(
            ConfirmarPagoSuscripcionDto dto, int? usuarioIdSolicitante = null)
        {
            await using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                var transaccion = await _context.Transacciones
                    .FirstOrDefaultAsync(t => t.TransaccionId == dto.TransaccionId);

                if (transaccion == null)
                    return ResultadoOperacion<TransaccionDto>.SetError("Transacción no encontrada.");

                if (transaccion.Tipo != TipoTransaccion.PagoSuscripcion)
                    return ResultadoOperacion<TransaccionDto>.SetError("Tipo de transacción no válido.");

                if (transaccion.Estado == TipoEstadoTransaccion.Completada)
                    return ResultadoOperacion<TransaccionDto>.SetExito(transaccion.ToDto());

                if (transaccion.Estado != TipoEstadoTransaccion.Pendiente &&
                    transaccion.Estado != TipoEstadoTransaccion.Procesando)
                    return ResultadoOperacion<TransaccionDto>.SetError("Esta transacción ya no puede ser modificada.");

                var autorizado = false;
                if (usuarioIdSolicitante.HasValue)
                {
                    if (transaccion.UsuarioId != usuarioIdSolicitante.Value)
                        return ResultadoOperacion<TransaccionDto>.SetError("No autorizado para confirmar esta transacción.");
                    autorizado = string.Equals(
                        _config["Izipay:PermitirConfirmacionDemo"],
                        "true",
                        StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    autorizado = await ValidarRespuestaIzipayAsync(dto);
                }

                if (!autorizado)
                {
                    transaccion.Estado = TipoEstadoTransaccion.Fallida;
                    transaccion.RespuestaProveedor = dto.RespuestaProveedor;
                    await _context.SaveChangesAsync();
                    await trx.CommitAsync();
                    return ResultadoOperacion<TransaccionDto>.SetError("La respuesta del pago no es válida.");
                }

                transaccion.TransaccionProveedorId = dto.TransaccionProveedorId;
                transaccion.MetodoPago = dto.MetodoPago;
                transaccion.RespuestaProveedor = dto.RespuestaProveedor;
                transaccion.Estado = dto.Exitosa
                    ? TipoEstadoTransaccion.Completada
                    : TipoEstadoTransaccion.Fallida;
                transaccion.Fecha = DateTime.UtcNow;

                if (dto.Exitosa && transaccion.SuscripcionId.HasValue)
                    await ActivarSuscripcionTrasPagoAsync(transaccion.SuscripcionId.Value, transaccion.UsuarioId);

                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                return ResultadoOperacion<TransaccionDto>.SetExito(transaccion.ToDto());
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                return ResultadoOperacion<TransaccionDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<List<TransaccionDto>>> ListarMisTransaccionesAsync(int usuarioId)
        {
            try
            {
                var lista = await _context.Transacciones.AsNoTracking()
                    .Where(t => t.UsuarioId == usuarioId && t.Tipo == TipoTransaccion.PagoSuscripcion)
                    .OrderByDescending(t => t.TransaccionId)
                    .ToListAsync();
                return ResultadoOperacion<List<TransaccionDto>>.SetExito(lista.Select(t => t.ToDto()).ToList());
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<List<TransaccionDto>>.SetError("Error: " + ex.Message);
            }
        }

        private static decimal CalcularMontoConDescuento(Suscripcion sus)
        {
            var precio = sus.PrecioPersonalizado ?? sus.Plan.Precio;
            if (sus.Cupon == null)
                return precio;

            return CuponServicio.CalcularPrecioConDescuento(precio, sus.Cupon);
        }

        private async Task ActivarSuscripcionTrasPagoAsync(int suscripcionId, int usuarioId)
        {
            var sus = await _context.Suscripciones.Include(s => s.Plan)
                .FirstAsync(s => s.SuscripcionId == suscripcionId);

            var now = DateTime.UtcNow;
            sus.Estado = TipoEstadoSuscripcion.Activa;
            var inicioNuevoPeriodo = sus.PeriodoFin.HasValue && sus.PeriodoFin > now
                ? sus.PeriodoFin.Value
                : now;
            sus.PeriodoInicio = inicioNuevoPeriodo;
            sus.PeriodoFin = inicioNuevoPeriodo.AddMonths((int)sus.Plan.Periodo);

            _context.Notificaciones.Add(new Notificacion
            {
                UsuarioId = usuarioId,
                Tipo = "SUSCRIPCION_PAGADA",
                Titulo = "¡Pago confirmado!",
                Cuerpo = $"Tu suscripción al plan {sus.Plan.Nombre} está activa hasta {sus.PeriodoFin:dd/MM/yyyy}.",
                Leida = false,
                Fecha = now
            });
        }

        private Task<(string formToken, string publicKey)> GenerarFormTokenIzipayAsync(
            Transaccion transaccion, Suscripcion sus)
        {
            var formTokenPlaceholder = $"DEMO-FORM-TOKEN-{transaccion.TransaccionId}-{Guid.NewGuid():N}";
            var publicKey = _config["Izipay:PublicKey"] ?? "DEMO-PUBLIC-KEY";
            return Task.FromResult((formTokenPlaceholder, publicKey));
        }

        private Task<bool> ValidarRespuestaIzipayAsync(ConfirmarPagoSuscripcionDto dto)
        {
            // TODO: validar kr-answer y kr-hash con HMAC-SHA256 (clave Izipay:HmacKey)
            return Task.FromResult(false);
        }
    }
}
