using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.PagoXqm;
using TiendaVirtual.Dominio.Servicios.SoporteXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Servicios.VentaXqm.Implementacion
{
    /// <summary>
    /// Cobro de órdenes de compra. Modo demo: genera formToken simulado hasta integrar Izipay real.
    /// </summary>
    public class OrdenPagoServicio : IOrdenPagoServicio
    {
        private readonly TiendaVirtualDbContext _context;
        private readonly IConfiguration _config;
        private readonly INotificacionServicio _notificacionServicio;

        public OrdenPagoServicio(
            TiendaVirtualDbContext context,
            IConfiguration config,
            INotificacionServicio notificacionServicio)
        {
            _context = context;
            _config = config;
            _notificacionServicio = notificacionServicio;
        }

        public async Task<ResultadoOperacion<RespuestaInicioPagoOrdenDto>> IniciarPagoAsync(
            int usuarioId, IniciarPagoOrdenDto dto)
        {
            await using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                var orden = await _context.Ordenes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.OrdenId == dto.OrdenId && o.ClienteId == usuarioId);

                if (orden == null)
                    return ResultadoOperacion<RespuestaInicioPagoOrdenDto>.SetError("Orden no encontrada.");

                if (orden.Estado != TipoEstadoOrden.PendientePago)
                    return ResultadoOperacion<RespuestaInicioPagoOrdenDto>.SetError(
                        "Solo se puede pagar una orden en estado PendientePago.");

                var transaccionExistente = await _context.Transacciones
                    .Where(t => t.OrdenId == orden.OrdenId &&
                                t.Tipo == TipoTransaccion.PagoOrden &&
                                (t.Estado == TipoEstadoTransaccion.Pendiente ||
                                 t.Estado == TipoEstadoTransaccion.Procesando))
                    .OrderByDescending(t => t.TransaccionId)
                    .FirstOrDefaultAsync();

                Transaccion transaccion;
                if (transaccionExistente != null && transaccionExistente.Monto == orden.Total)
                {
                    transaccion = transaccionExistente;
                }
                else
                {
                    if (transaccionExistente != null)
                        transaccionExistente.Estado = TipoEstadoTransaccion.Cancelada;

                    transaccion = new Transaccion
                    {
                        OrdenId = orden.OrdenId,
                        UsuarioId = usuarioId,
                        Proveedor = "IZIPAY",
                        Tipo = TipoTransaccion.PagoOrden,
                        Monto = orden.Total,
                        Estado = TipoEstadoTransaccion.Pendiente,
                        Fecha = DateTime.UtcNow
                    };
                    _context.Transacciones.Add(transaccion);
                    await _context.SaveChangesAsync();
                }

                var (formToken, publicKey) = GenerarFormTokenDemo(transaccion, orden.NumeroOrden);
                await trx.CommitAsync();

                return ResultadoOperacion<RespuestaInicioPagoOrdenDto>.SetExito(new RespuestaInicioPagoOrdenDto
                {
                    TransaccionId = transaccion.TransaccionId,
                    Monto = transaccion.Monto,
                    Moneda = "PEN",
                    Concepto = $"Pedido {orden.NumeroOrden}",
                    FormToken = formToken,
                    PublicKey = publicKey
                });
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                return ResultadoOperacion<RespuestaInicioPagoOrdenDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<TransaccionOrdenDto>> ConfirmarPagoAsync(
            ConfirmarPagoOrdenDto dto, int? usuarioIdSolicitante = null)
        {
            await using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                var transaccion = await _context.Transacciones
                    .FirstOrDefaultAsync(t => t.TransaccionId == dto.TransaccionId);

                if (transaccion == null)
                    return ResultadoOperacion<TransaccionOrdenDto>.SetError("Transacción no encontrada.");

                if (transaccion.Tipo != TipoTransaccion.PagoOrden)
                    return ResultadoOperacion<TransaccionOrdenDto>.SetError("Tipo de transacción no válido.");

                if (transaccion.Estado == TipoEstadoTransaccion.Completada)
                    return ResultadoOperacion<TransaccionOrdenDto>.SetExito(transaccion.ToOrdenDto());

                if (transaccion.Estado != TipoEstadoTransaccion.Pendiente &&
                    transaccion.Estado != TipoEstadoTransaccion.Procesando)
                    return ResultadoOperacion<TransaccionOrdenDto>.SetError("Esta transacción ya no puede ser modificada.");

                var autorizado = false;
                if (usuarioIdSolicitante.HasValue)
                {
                    if (transaccion.UsuarioId != usuarioIdSolicitante.Value)
                        return ResultadoOperacion<TransaccionOrdenDto>.SetError("No autorizado para confirmar esta transacción.");
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
                    return ResultadoOperacion<TransaccionOrdenDto>.SetError("La respuesta del pago no es válida.");
                }

                transaccion.TransaccionProveedorId = dto.TransaccionProveedorId;
                transaccion.MetodoPago = dto.MetodoPago;
                transaccion.RespuestaProveedor = dto.RespuestaProveedor;
                transaccion.Estado = dto.Exitosa
                    ? TipoEstadoTransaccion.Completada
                    : TipoEstadoTransaccion.Fallida;
                transaccion.Fecha = DateTime.UtcNow;

                string? tituloCliente = null;
                string? cuerpoCliente = null;
                var notifsVendedores = new List<(int UsuarioId, string Titulo, string Cuerpo)>();

                if (dto.Exitosa && transaccion.OrdenId.HasValue)
                {
                    (tituloCliente, cuerpoCliente, notifsVendedores) =
                        await ActivarOrdenTrasPagoAsync(transaccion.OrdenId.Value);
                }

                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                if (tituloCliente != null && cuerpoCliente != null)
                {
                    await _notificacionServicio.CrearAsync(
                        transaccion.UsuarioId,
                        TipoNotificacion.OrdenPagada,
                        tituloCliente,
                        cuerpoCliente,
                        new { ordenId = transaccion.OrdenId });
                }

                foreach (var (vendedorUsuarioId, titulo, cuerpo) in notifsVendedores)
                {
                    await _notificacionServicio.CrearAsync(
                        vendedorUsuarioId,
                        TipoNotificacion.SubordenEnPreparacion,
                        titulo,
                        cuerpo);
                }

                return ResultadoOperacion<TransaccionOrdenDto>.SetExito(transaccion.ToOrdenDto());
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                return ResultadoOperacion<TransaccionOrdenDto>.SetError("Error: " + ex.Message);
            }
        }

        private async Task<(string? TituloCliente, string? CuerpoCliente, List<(int, string, string)> NotifsVendedor)>
            ActivarOrdenTrasPagoAsync(long ordenId)
        {
            var orden = await _context.Ordenes
                .Include(o => o.Subordenes)
                .ThenInclude(s => s.Vendedor)
                .FirstAsync(o => o.OrdenId == ordenId);

            if (orden.Estado != TipoEstadoOrden.PendientePago)
                return (null, null, new List<(int, string, string)>());

            orden.Estado = TipoEstadoOrden.Pagada;

            var notifsVendedor = new List<(int, string, string)>();
            foreach (var sub in orden.Subordenes.Where(s => s.Estado == TipoEstadoSuborden.Pendiente))
            {
                sub.Estado = TipoEstadoSuborden.EnPreparacion;
                notifsVendedor.Add((
                    sub.Vendedor.UsuarioId,
                    "Pedido pagado — preparar envío",
                    $"El pedido {sub.NumeroSuborden} fue pagado. Puedes comenzar a prepararlo."));
            }

            return (
                "¡Pago confirmado!",
                $"Tu pedido {orden.NumeroOrden} fue pagado correctamente. Los artesanos comenzarán a prepararlo.",
                notifsVendedor);
        }

        private (string formToken, string publicKey) GenerarFormTokenDemo(Transaccion transaccion, string numeroOrden)
        {
            var formToken = $"DEMO-ORDEN-FORM-TOKEN-{transaccion.TransaccionId}-{Guid.NewGuid():N}";
            var publicKey = _config["Izipay:PublicKey"] ?? "DEMO-PUBLIC-KEY";
            return (formToken, publicKey);
        }

        private Task<bool> ValidarRespuestaIzipayAsync(ConfirmarPagoOrdenDto dto)
        {
            // TODO: validar kr-answer y kr-hash con HMAC-SHA256 (clave Izipay:HmacKey)
            return Task.FromResult(false);
        }
    }

    internal static class TransaccionOrdenMapping
    {
        public static TransaccionOrdenDto ToOrdenDto(this Transaccion entidad) =>
            new()
            {
                TransaccionId = entidad.TransaccionId,
                OrdenId = entidad.OrdenId,
                Proveedor = entidad.Proveedor,
                TransaccionProveedorId = entidad.TransaccionProveedorId,
                Tipo = new EnumeracionDto
                {
                    Id = (int)entidad.Tipo,
                    Nombre = entidad.Tipo.GetDescription()
                },
                Monto = entidad.Monto,
                Estado = new EnumeracionDto
                {
                    Id = (int)entidad.Estado,
                    Nombre = entidad.Estado.GetDescription()
                },
                MetodoPago = entidad.MetodoPago,
                Fecha = entidad.Fecha
            };
    }
}
