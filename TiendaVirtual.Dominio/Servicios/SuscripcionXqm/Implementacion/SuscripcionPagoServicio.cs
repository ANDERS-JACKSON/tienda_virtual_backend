using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Extensiones.PagoXqm;
using TiendaVirtual.Dominio.Modelo.PagoXqm;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Dominio.Opciones;
using TiendaVirtual.Dominio.Servicios.PagoXqm;
using TiendaVirtual.Dominio.Servicios.PagoXqm.Modelos;
using TiendaVirtual.Dominio.Servicios.SoporteXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.PagoXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Servicios.SuscripcionXqm.Implementacion
{
    public class SuscripcionPagoServicio : ISuscripcionPagoServicio
    {
        private readonly TiendaVirtualDbContext _context;
        private readonly ILogger<SuscripcionPagoServicio> _logger;
        private readonly INotificacionServicio _notificacionServicio;
        private readonly IProveedorPagoFactory _proveedorFactory;
        private readonly IzipayOpciones _izipay;
        private readonly IHostEnvironment _env;

        public SuscripcionPagoServicio(
            TiendaVirtualDbContext context,
            INotificacionServicio notificacionServicio,
            IProveedorPagoFactory proveedorFactory,
            IOptions<IzipayOpciones> izipay,
            IHostEnvironment env,
            ILogger<SuscripcionPagoServicio> logger)
        {
            _context = context;
            _notificacionServicio = notificacionServicio;
            _proveedorFactory = proveedorFactory;
            _izipay = izipay.Value;
            _env = env;
            _logger = logger;
        }

        public async Task<ResultadoOperacion<RespuestaInicioPagoDto>> IniciarPagoAsync(
            Guid usuarioId, IniciarPagoSuscripcionDto dto)
        {
            await using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                var vendedor = await _context.Vendedores
                    .Include(v => v.Usuario)
                    .FirstOrDefaultAsync(v => v.UsuarioId == usuarioId);
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
                var proveedor = _proveedorFactory.ObtenerActivo();

                var transaccionExistente = await _context.Transacciones
                    .Where(t => t.SuscripcionId == sus.SuscripcionId &&
                                t.Tipo == TipoTransaccion.PagoSuscripcion &&
                                (t.Estado == TipoEstadoTransaccion.Pendiente ||
                                 t.Estado == TipoEstadoTransaccion.Procesando))
                    .OrderByDescending(t => t.TransaccionId)
                    .FirstOrDefaultAsync();

                Transaccion transaccion;
                if (transaccionExistente != null &&
                    transaccionExistente.Monto == monto &&
                    string.Equals(transaccionExistente.Proveedor, proveedor.CodigoProveedor, StringComparison.OrdinalIgnoreCase))
                {
                    transaccion = transaccionExistente;
                }
                else
                {
                    if (transaccionExistente != null)
                        transaccionExistente.Estado = TipoEstadoTransaccion.Cancelada;

                    transaccion = new Transaccion
                    {
                        TransaccionId = Guid.NewGuid(),
                        SuscripcionId = sus.SuscripcionId,
                        UsuarioId = usuarioId,
                        Proveedor = proveedor.CodigoProveedor,
                        Tipo = TipoTransaccion.PagoSuscripcion,
                        Monto = monto,
                        Estado = TipoEstadoTransaccion.Pendiente,
                        Fecha = DateTime.UtcNow,
                    };
                    _context.Transacciones.Add(transaccion);
                    await _context.SaveChangesAsync();
                }

                var concepto = $"Suscripción {sus.Plan.Nombre} - {sus.Plan.Codigo}";
                var prep = await proveedor.PrepararCheckoutAsync(new PreparacionCheckoutSolicitud
                {
                    TransaccionId = transaccion.TransaccionId,
                    Monto = transaccion.Monto,
                    Moneda = "PEN",
                    Concepto = concepto,
                    EmailCliente = vendedor.Usuario.Correo,
                });

                await trx.CommitAsync();

                return ResultadoOperacion<RespuestaInicioPagoDto>.SetExito(new RespuestaInicioPagoDto
                {
                    TransaccionId = transaccion.TransaccionId,
                    Monto = transaccion.Monto,
                    Moneda = "PEN",
                    Concepto = concepto,
                    Proveedor = prep.CodigoProveedor,
                    FormToken = prep.FormToken,
                    PublicKey = prep.PublicKey,
                    RequiereTokenizacionCliente = prep.RequiereTokenizacionCliente,
                    PermiteConfirmacionDemo = prep.PermiteConfirmacionDemo,
                });
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                _logger.LogError(ex, "Error en SuscripcionPagoServicio.IniciarPagoAsync");
                return ResultadoOperacion<RespuestaInicioPagoDto>.SetError(
                    "Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        public async Task<ResultadoOperacion<ResultadoProcesarPagoOrdenDto>> ProcesarPagoAsync(
            Guid usuarioId, ProcesarPagoSuscripcionDto dto)
        {
            try
            {
                var transaccion = await _context.Transacciones
                    .FirstOrDefaultAsync(t => t.TransaccionId == dto.TransaccionId);

                if (transaccion == null)
                    return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError("Transacción no encontrada.");

                if (transaccion.UsuarioId != usuarioId)
                    return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError("No autorizado.");

                if (transaccion.Tipo != TipoTransaccion.PagoSuscripcion)
                    return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError("Tipo de transacción no válido.");

                if (transaccion.Estado == TipoEstadoTransaccion.Completada)
                    return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetExito(MapearProcesar(transaccion, true));

                if (transaccion.Estado is not (TipoEstadoTransaccion.Pendiente or TipoEstadoTransaccion.Procesando))
                    return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError("Esta transacción ya no puede ser modificada.");

                // Anti doble cobro: si ya hay un cobro en curso, solo consultar (como OrdenPagoServicio).
                if (transaccion.Estado == TipoEstadoTransaccion.Procesando)
                {
                    if (!string.IsNullOrWhiteSpace(transaccion.TransaccionProveedorId))
                    {
                        var proveedorConsulta = _proveedorFactory.ObtenerPorCodigo(transaccion.Proveedor);
                        var estadoActual = await proveedorConsulta.ConsultarPagoAsync(
                            transaccion.TransaccionProveedorId);

                        if (estadoActual.ConsultaFallida)
                            return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError(
                                "No pudimos verificar tu pago anterior. Intenta de nuevo en unos segundos.");

                        var aplicadoPrevio = await AplicarResultadoProveedorAsync(new ResultadoPagoVerificadoDto
                        {
                            TransaccionId = transaccion.TransaccionId,
                            IdPagoExterno = estadoActual.IdPagoExterno ?? transaccion.TransaccionProveedorId,
                            Exitoso = estadoActual.Exitoso,
                            Pendiente = estadoActual.Pendiente,
                            MontoConfirmado = estadoActual.MontoConfirmado ?? transaccion.Monto,
                            MetodoPago = estadoActual.MetodoPago ?? dto.PaymentMethodId,
                            EstadoExterno = estadoActual.EstadoExterno,
                            StatusDetail = estadoActual.StatusDetail,
                            RespuestaCruda = estadoActual.RespuestaCruda,
                        });

                        if (!aplicadoPrevio.Exito || aplicadoPrevio.Datos == null)
                            return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError(
                                aplicadoPrevio.Mensaje ?? "No se pudo confirmar el pago anterior.");

                        return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetExito(
                            new ResultadoProcesarPagoOrdenDto
                            {
                                TransaccionId = aplicadoPrevio.Datos.TransaccionId,
                                Proveedor = aplicadoPrevio.Datos.Proveedor,
                                TransaccionProveedorId = aplicadoPrevio.Datos.TransaccionProveedorId,
                                Estado = aplicadoPrevio.Datos.Estado,
                                Monto = aplicadoPrevio.Datos.Monto,
                                MetodoPago = aplicadoPrevio.Datos.MetodoPago,
                                EstadoExterno = estadoActual.EstadoExterno,
                                StatusDetail = estadoActual.StatusDetail,
                                MensajeUsuario = estadoActual.Exitoso
                                    ? "Pago acreditado correctamente."
                                    : estadoActual.Pendiente
                                        ? MensajesRechazoMercadoPago.ParaUsuario(
                                            estadoActual.StatusDetail, estadoActual.MetodoPago)
                                        : MensajesRechazoMercadoPago.ParaUsuario(
                                            estadoActual.StatusDetail, estadoActual.MetodoPago),
                                Pagado = aplicadoPrevio.Datos.Estado.Id == (int)TipoEstadoTransaccion.Completada,
                                Pendiente = estadoActual.Pendiente && !estadoActual.Exitoso,
                            });
                    }

                    return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError(
                        "Ya hay un cobro en curso para esta suscripción. Espera unos segundos e intenta de nuevo.");
                }

                var sus = await _context.Suscripciones
                    .Include(s => s.Plan)
                    .FirstOrDefaultAsync(s => s.SuscripcionId == transaccion.SuscripcionId);

                if (sus == null || sus.Estado != TipoEstadoSuscripcion.PendientePago)
                    return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError("La suscripción no admite cobro.");

                var usuario = await _context.Usuarios.AsNoTracking()
                    .FirstAsync(u => u.UsuarioId == usuarioId);

                var proveedor = _proveedorFactory.ObtenerPorCodigo(transaccion.Proveedor);
                var email = string.IsNullOrWhiteSpace(dto.PayerEmail) ? usuario.Correo : dto.PayerEmail;
                var idempotencyKey = $"suscripcion-{transaccion.TransaccionId:N}-{dto.PaymentMethodId}";

                transaccion.Estado = TipoEstadoTransaccion.Procesando;
                await _context.SaveChangesAsync();

                var resultado = await proveedor.CrearPagoAsync(new SolicitudPagoDto
                {
                    TransaccionId = transaccion.TransaccionId,
                    IdempotencyKey = idempotencyKey,
                    Monto = transaccion.Monto,
                    Moneda = "PEN",
                    Descripcion = $"Suscripción {sus.Plan.Nombre}",
                    EmailPagador = email,
                    Token = dto.Token,
                    PaymentMethodId = dto.PaymentMethodId,
                    Installments = dto.Installments,
                    IssuerId = dto.IssuerId,
                    IdentificationType = dto.IdentificationType,
                    IdentificationNumber = dto.IdentificationNumber,
                });

                if (!string.IsNullOrEmpty(resultado.MensajeError) && resultado.IdPagoExterno == null)
                {
                    transaccion.Estado = TipoEstadoTransaccion.Fallida;
                    transaccion.RespuestaProveedor = resultado.RespuestaCruda;
                    await _context.SaveChangesAsync();
                    return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError(resultado.MensajeError);
                }

                if (resultado.MontoConfirmado.HasValue &&
                    Math.Round(resultado.MontoConfirmado.Value, 2) != Math.Round(transaccion.Monto, 2))
                {
                    _logger.LogError(
                        "Discrepancia de monto MP (suscripción). TransaccionId={Id}",
                        transaccion.TransaccionId);
                    transaccion.Estado = TipoEstadoTransaccion.Fallida;
                    transaccion.TransaccionProveedorId = resultado.IdPagoExterno;
                    transaccion.RespuestaProveedor = JsonSerializer.Serialize(new
                    {
                        alerta = "monto_mismatch",
                        esperado = transaccion.Monto,
                        recibido = resultado.MontoConfirmado,
                    });
                    await _context.SaveChangesAsync();
                    return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError(
                        "El monto del pago no coincide. Contacta a soporte.");
                }

                if (resultado.Exitoso)
                {
                    var aplicado = await AplicarResultadoProveedorAsync(new ResultadoPagoVerificadoDto
                    {
                        TransaccionId = transaccion.TransaccionId,
                        IdPagoExterno = resultado.IdPagoExterno!,
                        Exitoso = true,
                        MontoConfirmado = resultado.MontoConfirmado ?? transaccion.Monto,
                        MetodoPago = resultado.MetodoPago ?? dto.PaymentMethodId,
                        EstadoExterno = resultado.EstadoExterno,
                        StatusDetail = resultado.StatusDetail,
                        RespuestaCruda = resultado.RespuestaCruda,
                    });

                    if (!aplicado.Exito || aplicado.Datos == null)
                        return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError(
                            aplicado.Mensaje ?? "No se pudo confirmar el pago.");

                    return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetExito(new ResultadoProcesarPagoOrdenDto
                    {
                        TransaccionId = aplicado.Datos.TransaccionId,
                        Proveedor = aplicado.Datos.Proveedor,
                        TransaccionProveedorId = aplicado.Datos.TransaccionProveedorId,
                        Estado = aplicado.Datos.Estado,
                        Monto = aplicado.Datos.Monto,
                        MetodoPago = aplicado.Datos.MetodoPago,
                        EstadoExterno = resultado.EstadoExterno,
                        StatusDetail = resultado.StatusDetail,
                        MensajeUsuario = "Pago acreditado correctamente.",
                        Pagado = true,
                    });
                }

                await _context.Entry(transaccion).ReloadAsync();
                if (transaccion.Estado == TipoEstadoTransaccion.Completada)
                    return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetExito(MapearProcesar(transaccion, true));

                transaccion.TransaccionProveedorId = resultado.IdPagoExterno;
                transaccion.MetodoPago = resultado.MetodoPago ?? dto.PaymentMethodId;
                transaccion.RespuestaProveedor = resultado.RespuestaCruda;
                transaccion.Estado = resultado.Pendiente
                    ? TipoEstadoTransaccion.Procesando
                    : TipoEstadoTransaccion.Fallida;
                transaccion.Fecha = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var msg = MensajesRechazoMercadoPago.ParaUsuario(resultado.StatusDetail);
                var dtoOut = MapearProcesar(transaccion, false, resultado.Pendiente, resultado.StatusDetail, msg);

                if (resultado.Pendiente)
                    return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetExito(dtoOut);

                return new ResultadoOperacion<ResultadoProcesarPagoOrdenDto>
                {
                    Exito = false,
                    Mensaje = msg,
                    Datos = dtoOut,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en SuscripcionPagoServicio.ProcesarPagoAsync");
                return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError(
                    "Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        public async Task<ResultadoOperacion<TransaccionDto>> ConfirmarPagoAsync(
            ConfirmarPagoSuscripcionDto dto, Guid? usuarioIdSolicitante = null)
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

                if (transaccion.Estado is not (TipoEstadoTransaccion.Pendiente or TipoEstadoTransaccion.Procesando))
                    return ResultadoOperacion<TransaccionDto>.SetError("Esta transacción ya no puede ser modificada.");

                if (string.Equals(transaccion.Proveedor, CodigoProveedorPago.MercadoPago, StringComparison.OrdinalIgnoreCase))
                {
                    return ResultadoOperacion<TransaccionDto>.SetError(
                        "Los pagos de Mercado Pago se confirman automáticamente. No uses este endpoint.");
                }

                var autorizado = false;
                if (usuarioIdSolicitante.HasValue)
                {
                    if (transaccion.UsuarioId != usuarioIdSolicitante.Value)
                        return ResultadoOperacion<TransaccionDto>.SetError("No autorizado para confirmar esta transacción.");

                    if (_izipay.PermitirConfirmacionDemo)
                    {
                        if (!_env.IsDevelopment())
                        {
                            _logger.LogCritical("Izipay demo confirmación bloqueada fuera de Development.");
                            return ResultadoOperacion<TransaccionDto>.SetError(
                                "Confirmación demo deshabilitada en este ambiente.");
                        }

                        autorizado = true;
                    }
                }

                if (!autorizado)
                {
                    transaccion.Estado = TipoEstadoTransaccion.Fallida;
                    transaccion.RespuestaProveedor = dto.RespuestaProveedor;
                    await _context.SaveChangesAsync();
                    await trx.CommitAsync();
                    return ResultadoOperacion<TransaccionDto>.SetError("La respuesta del pago no es válida.");
                }

                await trx.CommitAsync();

                return await AplicarResultadoProveedorAsync(new ResultadoPagoVerificadoDto
                {
                    TransaccionId = dto.TransaccionId,
                    IdPagoExterno = dto.TransaccionProveedorId,
                    Exitoso = dto.Exitosa,
                    MontoConfirmado = transaccion.Monto,
                    MetodoPago = dto.MetodoPago,
                    EstadoExterno = dto.Exitosa ? "approved" : "rejected",
                    RespuestaCruda = dto.RespuestaProveedor,
                });
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                _logger.LogError(ex, "Error en SuscripcionPagoServicio.ConfirmarPagoAsync");
                return ResultadoOperacion<TransaccionDto>.SetError(
                    "Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        public async Task<ResultadoOperacion<TransaccionDto>> AplicarResultadoProveedorAsync(
            ResultadoPagoVerificadoDto resultado)
        {
            await using var trx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var transaccion = await _context.Transacciones
                    .FirstOrDefaultAsync(t => t.TransaccionId == resultado.TransaccionId);

                if (transaccion == null)
                    return ResultadoOperacion<TransaccionDto>.SetError("Transacción no encontrada.");

                if (transaccion.Tipo != TipoTransaccion.PagoSuscripcion)
                    return ResultadoOperacion<TransaccionDto>.SetError("Tipo de transacción no válido.");

                if (transaccion.Estado == TipoEstadoTransaccion.Completada)
                    return ResultadoOperacion<TransaccionDto>.SetExito(transaccion.ToDto());

                if (transaccion.Estado is not (TipoEstadoTransaccion.Pendiente or TipoEstadoTransaccion.Procesando))
                    return ResultadoOperacion<TransaccionDto>.SetError("Esta transacción ya no puede ser modificada.");

                if (Math.Round(resultado.MontoConfirmado, 2) != Math.Round(transaccion.Monto, 2))
                {
                    _logger.LogError(
                        "Monto mismatch suscripción. TransaccionId={Id}",
                        transaccion.TransaccionId);
                    transaccion.Estado = TipoEstadoTransaccion.Fallida;
                    transaccion.TransaccionProveedorId = resultado.IdPagoExterno;
                    transaccion.RespuestaProveedor = resultado.RespuestaCruda;
                    await _context.SaveChangesAsync();
                    await trx.CommitAsync();
                    return ResultadoOperacion<TransaccionDto>.SetError("Discrepancia de monto; pago no aplicado.");
                }

                if (resultado.Pendiente && !resultado.Exitoso)
                {
                    transaccion.Estado = TipoEstadoTransaccion.Procesando;
                    transaccion.TransaccionProveedorId = resultado.IdPagoExterno;
                    transaccion.MetodoPago = resultado.MetodoPago;
                    transaccion.RespuestaProveedor = resultado.RespuestaCruda;
                    await _context.SaveChangesAsync();
                    await trx.CommitAsync();
                    return ResultadoOperacion<TransaccionDto>.SetExito(transaccion.ToDto());
                }

                transaccion.TransaccionProveedorId = resultado.IdPagoExterno;
                transaccion.MetodoPago = resultado.MetodoPago;
                transaccion.RespuestaProveedor = resultado.RespuestaCruda;
                transaccion.Estado = resultado.Exitoso
                    ? TipoEstadoTransaccion.Completada
                    : TipoEstadoTransaccion.Fallida;
                transaccion.Fecha = DateTime.UtcNow;

                string? tituloNotif = null;
                string? cuerpoNotif = null;

                if (resultado.Exitoso && transaccion.SuscripcionId.HasValue)
                {
                    (tituloNotif, cuerpoNotif) = await ActivarSuscripcionTrasPagoAsync(
                        transaccion.SuscripcionId.Value);
                }

                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                if (tituloNotif != null && cuerpoNotif != null)
                {
                    await _notificacionServicio.CrearAsync(
                        transaccion.UsuarioId,
                        TipoNotificacion.SuscripcionPagada,
                        tituloNotif,
                        cuerpoNotif);
                }

                return ResultadoOperacion<TransaccionDto>.SetExito(transaccion.ToDto());
            }
            catch (PostgresException pgEx) when (pgEx.SqlState is "40001" or "40P01")
            {
                await trx.RollbackAsync();
                _logger.LogWarning(pgEx,
                    "Conflicto de concurrencia (serialización) al aplicar resultado de pago. TransaccionId={Id}",
                    resultado.TransaccionId);

                var actual = await _context.Transacciones.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TransaccionId == resultado.TransaccionId);

                return actual != null
                    ? ResultadoOperacion<TransaccionDto>.SetExito(actual.ToDto())
                    : ResultadoOperacion<TransaccionDto>.SetError(
                        "No se pudo confirmar el pago. Intenta de nuevo.");
            }
            catch (DbUpdateException ex) when (EsConflictoDeConcurrencia(ex))
            {
                await trx.RollbackAsync();
                _logger.LogWarning(ex,
                    "Conflicto de concurrencia al aplicar resultado de pago (otro proceso ganó la carrera). TransaccionId={Id}",
                    resultado.TransaccionId);

                var actual = await _context.Transacciones.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TransaccionId == resultado.TransaccionId);

                return actual != null
                    ? ResultadoOperacion<TransaccionDto>.SetExito(actual.ToDto())
                    : ResultadoOperacion<TransaccionDto>.SetError(
                        "No se pudo confirmar el pago. Intenta de nuevo.");
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                _logger.LogError(ex, "Error en SuscripcionPagoServicio.AplicarResultadoProveedorAsync");
                return ResultadoOperacion<TransaccionDto>.SetError(
                    "Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        private static bool EsConflictoDeConcurrencia(DbUpdateException ex)
        {
            // PostgreSQL: 40001 = serialization_failure, 40P01 = deadlock_detected
            return ex.InnerException is PostgresException pgEx &&
                   (pgEx.SqlState == "40001" || pgEx.SqlState == "40P01");
        }

        public async Task<ResultadoOperacion<List<TransaccionDto>>> ListarMisTransaccionesAsync(Guid usuarioId)
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
                _logger.LogError(ex, "Error en SuscripcionPagoServicio.ListarMisTransaccionesAsync");
                return ResultadoOperacion<List<TransaccionDto>>.SetError(
                    "Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        private static decimal CalcularMontoConDescuento(Suscripcion sus)
        {
            var precio = sus.PrecioPersonalizado ?? sus.Plan.Precio;
            if (sus.Cupon == null)
                return precio;

            return CuponServicio.CalcularPrecioConDescuento(precio, sus.Cupon);
        }

        private async Task<(string Titulo, string Cuerpo)> ActivarSuscripcionTrasPagoAsync(int suscripcionId)
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

            return (
                "¡Pago confirmado!",
                $"Tu suscripción al plan {sus.Plan.Nombre} está activa hasta {sus.PeriodoFin:dd/MM/yyyy}.");
        }

        private static ResultadoProcesarPagoOrdenDto MapearProcesar(
            Transaccion t,
            bool pagado,
            bool pendiente = false,
            string? statusDetail = null,
            string? mensaje = null) =>
            new()
            {
                TransaccionId = t.TransaccionId,
                Proveedor = t.Proveedor,
                TransaccionProveedorId = t.TransaccionProveedorId,
                Estado = new EnumeracionDto
                {
                    Id = (int)t.Estado,
                    Nombre = t.Estado.GetDescription(),
                },
                Monto = t.Monto,
                MetodoPago = t.MetodoPago,
                EstadoExterno = statusDetail,
                StatusDetail = statusDetail,
                MensajeUsuario = mensaje,
                Pagado = pagado,
                Pendiente = pendiente,
            };
    }
}
