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
        private readonly MercadoPagoOpciones _mercadoPago;
        private readonly IHostEnvironment _env;

        public SuscripcionPagoServicio(
            TiendaVirtualDbContext context,
            INotificacionServicio notificacionServicio,
            IProveedorPagoFactory proveedorFactory,
            IOptions<IzipayOpciones> izipay,
            IOptions<MercadoPagoOpciones> mercadoPago,
            IHostEnvironment env,
            ILogger<SuscripcionPagoServicio> logger)
        {
            _context = context;
            _notificacionServicio = notificacionServicio;
            _proveedorFactory = proveedorFactory;
            _izipay = izipay.Value;
            _mercadoPago = mercadoPago.Value;
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

                var now = DateTime.UtcNow;
                if (SuscripcionBeneficiosHelper.AplicarVencimientoSiCorresponde(sus, now))
                    await _context.SaveChangesAsync();

                var (intencion, errorIntencion) = await ResolverIntencionCobroAsync(sus, dto.PlanDestinoId, now);
                if (intencion == null)
                    return ResultadoOperacion<RespuestaInicioPagoDto>.SetError(errorIntencion!);

                var proveedor = _proveedorFactory.ObtenerActivo();

                var transaccionExistente = await _context.Transacciones
                    .Where(t => t.SuscripcionId == sus.SuscripcionId &&
                                t.Tipo == TipoTransaccion.PagoSuscripcion &&
                                (t.Estado == TipoEstadoTransaccion.Pendiente ||
                                 t.Estado == TipoEstadoTransaccion.Procesando))
                    .OrderByDescending(t => t.Fecha)
                    .FirstOrDefaultAsync();

                if (transaccionExistente is { Estado: TipoEstadoTransaccion.Procesando } &&
                    !string.IsNullOrWhiteSpace(transaccionExistente.TransaccionProveedorId))
                {
                    return ResultadoOperacion<RespuestaInicioPagoDto>.SetError(
                        "Ya hay un pago en verificación para esta suscripción. Espera su confirmación antes de pagar de nuevo.");
                }

                Transaccion transaccion;
                if (transaccionExistente is { Estado: TipoEstadoTransaccion.Pendiente } &&
                    EsMismaIntencion(transaccionExistente, intencion, proveedor.CodigoProveedor, now))
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
                        Monto = intencion.Monto,
                        Estado = TipoEstadoTransaccion.Pendiente,
                        Fecha = now,
                        MotivoSuscripcion = intencion.Motivo,
                        PlanDestinoId = intencion.PlanDestino?.PlanId,
                    };
                    _context.Transacciones.Add(transaccion);
                    await _context.SaveChangesAsync();
                }

                var concepto = intencion.Concepto;
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
                    .Include(s => s.Cupon)
                    .FirstOrDefaultAsync(s => s.SuscripcionId == transaccion.SuscripcionId);

                if (sus == null)
                    return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError("La suscripción no admite cobro.");

                // El monto se recalcula en servidor: el cobro solo procede si la intención sigue vigente.
                var now = DateTime.UtcNow;
                SuscripcionBeneficiosHelper.AplicarVencimientoSiCorresponde(sus, now);
                var (intencion, errorIntencion) = await ResolverIntencionCobroAsync(sus, transaccion.PlanDestinoId, now);
                if (intencion == null ||
                    !EsMismaIntencion(transaccion, intencion, transaccion.Proveedor, now))
                {
                    transaccion.Estado = TipoEstadoTransaccion.Cancelada;
                    await _context.SaveChangesAsync();
                    return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError(
                        errorIntencion ?? "Las condiciones del cobro cambiaron. Vuelve a iniciar el pago.");
                }

                var esYape = string.Equals(dto.PaymentMethodId, "yape", StringComparison.OrdinalIgnoreCase);
                if (!esYape &&
                    (string.IsNullOrWhiteSpace(dto.IdentificationType) ||
                     string.IsNullOrWhiteSpace(dto.IdentificationNumber)))
                {
                    return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError(
                        "Para pagar con tarjeta indica tipo y número de documento.");
                }

                var usuario = await _context.Usuarios.AsNoTracking()
                    .FirstAsync(u => u.UsuarioId == usuarioId);

                var proveedor = _proveedorFactory.ObtenerPorCodigo(transaccion.Proveedor);
                var email = string.IsNullOrWhiteSpace(dto.PayerEmail) ? usuario.Correo : dto.PayerEmail;

                // Una sola clave por transacción → reintentos de red no generan otro cobro.
                var idempotencyKey = $"suscripcion-{transaccion.TransaccionId:N}";

                transaccion.Estado = TipoEstadoTransaccion.Procesando;
                await _context.SaveChangesAsync();

                var notificationUrl = ObtenerNotificationUrlMercadoPago(proveedor.CodigoProveedor);

                var resultado = await proveedor.CrearPagoAsync(new SolicitudPagoDto
                {
                    TransaccionId = transaccion.TransaccionId,
                    IdempotencyKey = idempotencyKey,
                    Monto = transaccion.Monto,
                    Moneda = "PEN",
                    Descripcion = intencion.Concepto,
                    EmailPagador = email,
                    Token = dto.Token,
                    PaymentMethodId = dto.PaymentMethodId,
                    Installments = 1,
                    IssuerId = dto.IssuerId,
                    IdentificationType = dto.IdentificationType,
                    IdentificationNumber = dto.IdentificationNumber,
                    NotificationUrl = notificationUrl,
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
                        "Discrepancia de monto MP (suscripción). TransaccionId={Id} Esperado={Esperado} Recibido={Recibido}",
                        transaccion.TransaccionId, transaccion.Monto, resultado.MontoConfirmado);

                    if (!string.IsNullOrWhiteSpace(resultado.IdPagoExterno))
                    {
                        try
                        {
                            var reembolso = await proveedor.ReembolsarAsync(resultado.IdPagoExterno);
                            _logger.LogWarning(
                                "Reembolso por monto mismatch (suscripción). Pago={Pago} Exito={Exito} Msg={Msg}",
                                resultado.IdPagoExterno, reembolso.Exitoso, reembolso.MensajeError);
                        }
                        catch (Exception refundEx)
                        {
                            _logger.LogError(refundEx,
                                "No se pudo reembolsar pago de suscripción con monto mismatch");
                        }
                    }

                    transaccion.Estado = TipoEstadoTransaccion.Fallida;
                    transaccion.TransaccionProveedorId = resultado.IdPagoExterno;
                    transaccion.RespuestaProveedor = JsonSerializer.Serialize(new
                    {
                        alerta = "monto_mismatch",
                        esperado = transaccion.Monto,
                        recibido = resultado.MontoConfirmado,
                        crudo = resultado.RespuestaCruda,
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
                        "Webhook/confirmación con monto distinto (suscripción). TransaccionId={Id} Esperado={E} Recibido={R}",
                        transaccion.TransaccionId, transaccion.Monto, resultado.MontoConfirmado);

                    if (!string.IsNullOrWhiteSpace(resultado.IdPagoExterno) &&
                        (resultado.Exitoso || resultado.Pendiente))
                    {
                        try
                        {
                            var proveedor = _proveedorFactory.ObtenerPorCodigo(transaccion.Proveedor);
                            var reembolso = await proveedor.ReembolsarAsync(resultado.IdPagoExterno);
                            _logger.LogWarning(
                                "Reembolso por monto mismatch (webhook suscripción). Pago={Pago} Exito={Exito}",
                                resultado.IdPagoExterno, reembolso.Exitoso);
                        }
                        catch (Exception refundEx)
                        {
                            _logger.LogError(refundEx,
                                "No se pudo reembolsar pago de suscripción con monto mismatch");
                        }
                    }

                    transaccion.Estado = TipoEstadoTransaccion.Fallida;
                    transaccion.TransaccionProveedorId = resultado.IdPagoExterno;
                    transaccion.RespuestaProveedor = JsonSerializer.Serialize(new
                    {
                        alerta = "monto_mismatch",
                        esperado = transaccion.Monto,
                        recibido = resultado.MontoConfirmado,
                        crudo = resultado.RespuestaCruda,
                    });
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

                PagoAplicado? pagoAplicado = null;
                if (resultado.Exitoso && transaccion.SuscripcionId.HasValue)
                    pagoAplicado = await ActivarSuscripcionTrasPagoAsync(transaccion);

                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                if (pagoAplicado != null)
                {
                    await _notificacionServicio.CrearAsync(
                        transaccion.UsuarioId,
                        TipoNotificacion.SuscripcionPagada,
                        "¡Pago confirmado!",
                        pagoAplicado.Mensaje,
                        plantillaEmail: PlantillaCorreo.SuscripcionPagada,
                        placeholdersEmail: pagoAplicado.PlaceholdersCorreo);
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

        public async Task<ResultadoOperacion<ResultadoVerificarPagoSuscripcionDto>> VerificarPagoSuscripcionAsync(
            Guid usuarioId, VerificarPagoSuscripcionDto dto)
        {
            try
            {
                var vendedor = await _context.Vendedores
                    .Include(v => v.Usuario)
                    .FirstOrDefaultAsync(v => v.UsuarioId == usuarioId);
                if (vendedor == null)
                    return ResultadoOperacion<ResultadoVerificarPagoSuscripcionDto>.SetError(
                        "No tienes perfil de vendedor.");

                var sus = await _context.Suscripciones
                    .Include(s => s.Plan)
                    .Include(s => s.Cupon)
                    .FirstOrDefaultAsync(s =>
                        s.SuscripcionId == dto.SuscripcionId && s.VendedorId == vendedor.VendedorId);

                if (sus == null)
                    return ResultadoOperacion<ResultadoVerificarPagoSuscripcionDto>.SetError(
                        "Suscripción no encontrada.");

                var now = DateTime.UtcNow;
                if (SuscripcionBeneficiosHelper.AplicarVencimientoSiCorresponde(sus, now))
                    await _context.SaveChangesAsync();

                var proveedor = _proveedorFactory.ObtenerActivo();

                var enCurso = await _context.Transacciones
                    .Where(t => t.SuscripcionId == sus.SuscripcionId &&
                                t.Tipo == TipoTransaccion.PagoSuscripcion &&
                                t.Estado == TipoEstadoTransaccion.Procesando)
                    .OrderByDescending(t => t.Fecha)
                    .FirstOrDefaultAsync();

                // Procesando sin id externo (fallo de red a mitad): liberar para reintento seguro.
                if (enCurso != null && string.IsNullOrWhiteSpace(enCurso.TransaccionProveedorId))
                {
                    enCurso.Estado = TipoEstadoTransaccion.Fallida;
                    enCurso.Fecha = now;
                    await _context.SaveChangesAsync();
                    enCurso = null;
                }

                string? mensajeIntentoAnterior = null;

                // Cobro en curso en el proveedor → solo consultar (nunca crear otro).
                if (enCurso != null)
                {
                    var proveedorConsulta = _proveedorFactory.ObtenerPorCodigo(enCurso.Proveedor);
                    var estado = await proveedorConsulta.ConsultarPagoAsync(enCurso.TransaccionProveedorId!);
                    if (estado.ConsultaFallida)
                    {
                        return ResultadoOperacion<ResultadoVerificarPagoSuscripcionDto>.SetExito(
                            ResultadoSinCheckout(sus.SuscripcionId, enCurso, pagado: false, pendiente: true,
                                "No pudimos verificar el estado de tu pago en este momento. Intenta de nuevo en unos segundos."));
                    }

                    var aplicado = await AplicarResultadoProveedorAsync(new ResultadoPagoVerificadoDto
                    {
                        TransaccionId = enCurso.TransaccionId,
                        IdPagoExterno = estado.IdPagoExterno ?? enCurso.TransaccionProveedorId!,
                        Exitoso = estado.Exitoso,
                        Pendiente = estado.Pendiente,
                        MontoConfirmado = estado.MontoConfirmado ?? enCurso.Monto,
                        MetodoPago = estado.MetodoPago ?? enCurso.MetodoPago,
                        EstadoExterno = estado.EstadoExterno,
                        StatusDetail = estado.StatusDetail,
                        RespuestaCruda = estado.RespuestaCruda,
                    });

                    var estadoTx = aplicado.Datos?.Estado.Id;
                    if (estadoTx == (int)TipoEstadoTransaccion.Completada)
                    {
                        return ResultadoOperacion<ResultadoVerificarPagoSuscripcionDto>.SetExito(
                            ResultadoSinCheckout(sus.SuscripcionId, enCurso, pagado: true, pendiente: false,
                                "Pago acreditado correctamente."));
                    }

                    if (estadoTx == (int)TipoEstadoTransaccion.Procesando)
                    {
                        // No cancelar: pending_contingency puede demorar; el webhook o una nueva verificación actualizan.
                        return ResultadoOperacion<ResultadoVerificarPagoSuscripcionDto>.SetExito(
                            ResultadoSinCheckout(sus.SuscripcionId, enCurso, pagado: false, pendiente: true,
                                MensajesRechazoMercadoPago.ParaUsuario(
                                    estado.StatusDetail, estado.MetodoPago ?? enCurso.MetodoPago)));
                    }

                    mensajeIntentoAnterior = aplicado.Mensaje
                        ?? "El pago anterior no se completó. Puedes intentar de nuevo.";
                }

                var (intencion, errorIntencion) = await ResolverIntencionCobroAsync(sus, dto.PlanDestinoId, now);
                if (intencion == null)
                {
                    if (!dto.PlanDestinoId.HasValue && SuscripcionBeneficiosHelper.EsComercialmenteActiva(sus, now))
                    {
                        return ResultadoOperacion<ResultadoVerificarPagoSuscripcionDto>.SetExito(
                            ResultadoSinCheckout(sus.SuscripcionId, null, pagado: true, pendiente: false,
                                $"Tu suscripción está al día. {errorIntencion}"));
                    }

                    return ResultadoOperacion<ResultadoVerificarPagoSuscripcionDto>.SetError(errorIntencion!);
                }

                var prep = await proveedor.PrepararCheckoutAsync(new PreparacionCheckoutSolicitud
                {
                    TransaccionId = Guid.Empty,
                    Monto = intencion.Monto,
                    Moneda = "PEN",
                    Concepto = intencion.Concepto,
                    EmailCliente = vendedor.Usuario.Correo,
                });

                return ResultadoOperacion<ResultadoVerificarPagoSuscripcionDto>.SetExito(
                    new ResultadoVerificarPagoSuscripcionDto
                    {
                        SuscripcionId = sus.SuscripcionId,
                        PuedeReintentarCobro = true,
                        MensajeUsuario = mensajeIntentoAnterior ?? "Puedes completar el pago de tu suscripción.",
                        Monto = intencion.Monto,
                        Proveedor = prep.CodigoProveedor,
                        PublicKey = prep.PublicKey,
                        RequiereTokenizacionCliente = prep.RequiereTokenizacionCliente,
                        PermiteConfirmacionDemo = prep.PermiteConfirmacionDemo,
                        Moneda = "PEN",
                        Concepto = intencion.Concepto,
                    });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Configuración de pago incompleta en VerificarPagoSuscripcionAsync");
                return ResultadoOperacion<ResultadoVerificarPagoSuscripcionDto>.SetError(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en SuscripcionPagoServicio.VerificarPagoSuscripcionAsync");
                return ResultadoOperacion<ResultadoVerificarPagoSuscripcionDto>.SetError(
                    "Ocurrió un error inesperado. Intente nuevamente.");
            }
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

        private string? ObtenerNotificationUrlMercadoPago(string codigoProveedor)
        {
            if (!string.Equals(codigoProveedor, CodigoProveedorPago.MercadoPago, StringComparison.OrdinalIgnoreCase))
                return null;

            var baseUrl = (_mercadoPago.UrlBase ?? string.Empty).Trim().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
                return null;

            // En local MP no puede llamar al webhook; omitir evita errores en create.
            if (baseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
                baseUrl.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))
                return null;

            return $"{baseUrl}/api/pagos/mercadopago/webhook";
        }

        /// <summary>Qué se cobra y cuánto. El monto siempre se calcula en servidor.</summary>
        private sealed record IntencionCobro(
            TipoMotivoPagoSuscripcion Motivo,
            decimal Monto,
            Plan? PlanDestino,
            string Concepto);

        private sealed record PagoAplicado(string Mensaje, Dictionary<string, string> PlaceholdersCorreo);

        /// <summary>
        /// Tiempo durante el que se respeta un monto cotizado. El prorrateo baja con el tiempo,
        /// así que un checkout abandonado debe volver a cotizarse.
        /// </summary>
        private static readonly TimeSpan VigenciaIntencionCobro = TimeSpan.FromMinutes(30);

        /// <remarks>
        /// Requiere <c>sus.Plan</c> y <c>sus.Cupon</c> cargados y el vencimiento ya aplicado.
        /// </remarks>
        private async Task<(IntencionCobro? Intencion, string? Error)> ResolverIntencionCobroAsync(
            Suscripcion sus, int? planDestinoId, DateTime now)
        {
            if (sus.Estado is not (TipoEstadoSuscripcion.EnPrueba
                or TipoEstadoSuscripcion.Activa
                or TipoEstadoSuscripcion.PendientePago))
                return (null, "Esta suscripción no admite cobro.");

            IntencionCobro intencion;
            if (planDestinoId.HasValue)
            {
                var destino = await _context.Planes.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PlanId == planDestinoId.Value && p.Activo);
                if (destino == null || destino.PlanId == sus.PlanId)
                    return (null, "El plan elegido no está disponible.");

                var cotizacion = CambioPlanCalculadora.Cotizar(sus, destino, now);
                if (!cotizacion.RequierePago)
                    return (null, "Este cambio de plan no requiere pago. Aplícalo desde la página de planes.");

                if (destino.MaxProductos is int max &&
                    await LimiteProductosPlanHelper.ContarEnUsoAsync(_context, sus.VendedorId) > max)
                    return (null, $"El plan {destino.Nombre} permite {max} productos. Pausa productos antes de cambiar.");

                var motivoUpgrade = cotizacion.MotivoCobro!.Value;
                intencion = new IntencionCobro(
                    motivoUpgrade, cotizacion.Monto, destino, ConceptoCobro(motivoUpgrade, destino.Nombre));
            }
            else
            {
                TipoMotivoPagoSuscripcion? motivo = sus.Estado switch
                {
                    TipoEstadoSuscripcion.PendientePago => TipoMotivoPagoSuscripcion.Renovacion,
                    TipoEstadoSuscripcion.EnPrueba when SuscripcionBeneficiosHelper.PuedePagarAnticipado(sus, now) =>
                        TipoMotivoPagoSuscripcion.AnticipadoPrueba,
                    TipoEstadoSuscripcion.Activa when SuscripcionBeneficiosHelper.PuedePagarAnticipado(sus, now) =>
                        TipoMotivoPagoSuscripcion.Renovacion,
                    _ => null,
                };

                if (motivo == null)
                    return (null,
                        $"Podrás renovar tu plan cuando falten {SuscripcionBeneficiosHelper.DiasVentanaRenovacion} " +
                        "días o menos para su vencimiento.");

                var monto = SuscripcionBeneficiosHelper.PrecioPorPeriodo(sus.Plan, sus.PrecioPersonalizado, sus.Cupon);
                intencion = new IntencionCobro(motivo.Value, monto, null, ConceptoCobro(motivo.Value, sus.Plan.Nombre));
            }

            if (intencion.Monto < SuscripcionBeneficiosHelper.MontoMinimoCobro)
                return (null, "El monto de este cobro no permite un pago en línea. Contacta a soporte.");

            return (intencion, null);
        }

        private static bool EsMismaIntencion(
            Transaccion transaccion, IntencionCobro intencion, string codigoProveedor, DateTime now)
        {
            if ((transaccion.MotivoSuscripcion ?? TipoMotivoPagoSuscripcion.Renovacion) != intencion.Motivo ||
                transaccion.PlanDestinoId != intencion.PlanDestino?.PlanId ||
                !string.Equals(transaccion.Proveedor, codigoProveedor, StringComparison.OrdinalIgnoreCase) ||
                now - transaccion.Fecha > VigenciaIntencionCobro)
                return false;

            // El prorrateo solo baja con el tiempo: dentro de la vigencia se respeta el monto cotizado.
            return intencion.Motivo == TipoMotivoPagoSuscripcion.UpgradeProrrateo
                ? intencion.Monto <= transaccion.Monto
                : intencion.Monto == transaccion.Monto;
        }

        private static string ConceptoCobro(TipoMotivoPagoSuscripcion motivo, string nombrePlan) => motivo switch
        {
            TipoMotivoPagoSuscripcion.AnticipadoPrueba => $"Plan {nombrePlan} - pago anticipado",
            TipoMotivoPagoSuscripcion.UpgradeProrrateo => $"Cambio al plan {nombrePlan} - diferencia prorrateada",
            TipoMotivoPagoSuscripcion.UpgradeDesdePrueba => $"Cambio al plan {nombrePlan}",
            _ => $"Suscripción plan {nombrePlan}",
        };

        private static ResultadoVerificarPagoSuscripcionDto ResultadoSinCheckout(
            int suscripcionId, Transaccion? transaccion, bool pagado, bool pendiente, string mensaje) =>
            new()
            {
                SuscripcionId = suscripcionId,
                Pagado = pagado,
                Pendiente = pendiente,
                PuedeReintentarCobro = false,
                MensajeUsuario = mensaje,
                TransaccionId = transaccion?.TransaccionId,
                Monto = transaccion?.Monto ?? 0m,
                Proveedor = transaccion?.Proveedor ?? string.Empty,
                Moneda = "PEN",
            };

        /// <summary>Aplica lo comprado según el motivo del cobro. El llamador guarda los cambios.</summary>
        private async Task<PagoAplicado> ActivarSuscripcionTrasPagoAsync(Transaccion transaccion)
        {
            var sus = await _context.Suscripciones
                .Include(s => s.Plan)
                .Include(s => s.Vendedor)
                .FirstAsync(s => s.SuscripcionId == transaccion.SuscripcionId);

            var now = DateTime.UtcNow;
            var motivo = transaccion.MotivoSuscripcion ?? TipoMotivoPagoSuscripcion.Renovacion;

            switch (motivo)
            {
                case TipoMotivoPagoSuscripcion.UpgradeDesdePrueba:
                case TipoMotivoPagoSuscripcion.UpgradeProrrateo:
                {
                    var destino = await _context.Planes.FirstAsync(p => p.PlanId == transaccion.PlanDestinoId);
                    CambioPlanCalculadora.AplicarPlan(sus, destino);

                    if (motivo == TipoMotivoPagoSuscripcion.UpgradeDesdePrueba)
                    {
                        if (sus.Estado == TipoEstadoSuscripcion.EnPrueba)
                            sus.PruebaTerminaEn = now;
                        IniciarPeriodoPagado(sus, now);
                    }
                    else if (!SuscripcionBeneficiosHelper.EsComercialmenteActiva(sus, now))
                    {
                        _logger.LogWarning(
                            "Upgrade prorrateado confirmado con el periodo ya vencido. SuscripcionId={Id} TransaccionId={Tx}",
                            sus.SuscripcionId, transaccion.TransaccionId);
                    }
                    break;
                }

                case TipoMotivoPagoSuscripcion.AnticipadoPrueba
                    when sus.Estado == TipoEstadoSuscripcion.EnPrueba && sus.PruebaTerminaEn > now:
                    sus.Estado = TipoEstadoSuscripcion.Activa;
                    sus.PeriodoInicio = now;
                    sus.PeriodoFin = sus.PruebaTerminaEn.Value.AddMonths(
                        SuscripcionBeneficiosHelper.MesesPorPeriodo(sus.Plan.Periodo));
                    sus.AvisoVencimientoEnviadoEn = null;
                    break;

                case TipoMotivoPagoSuscripcion.Renovacion
                    when sus.Estado == TipoEstadoSuscripcion.Activa && sus.PeriodoFin > now:
                    sus.PeriodoFin = sus.PeriodoFin.Value.AddMonths(
                        SuscripcionBeneficiosHelper.MesesPorPeriodo(sus.Plan.Periodo));
                    sus.AvisoVencimientoEnviadoEn = null;
                    break;

                default:
                    IniciarPeriodoPagado(sus, now);
                    break;
            }

            var vigenteHasta = sus.PeriodoFin.HasValue
                ? SuscripcionBeneficiosHelper.FormatoFechaPeru(sus.PeriodoFin.Value)
                : "-";
            var esUpgrade = motivo is TipoMotivoPagoSuscripcion.UpgradeDesdePrueba
                or TipoMotivoPagoSuscripcion.UpgradeProrrateo;
            var mensaje = esUpgrade
                ? $"Tu plan ahora es {sus.Plan.Nombre}. Está activo hasta {vigenteHasta}."
                : $"Tu suscripción al plan {sus.Plan.Nombre} está activa hasta {vigenteHasta}.";

            return new PagoAplicado(mensaje, new Dictionary<string, string>
            {
                ["vendedor"] = sus.Vendedor.NombreTienda,
                ["plan"] = sus.Plan.Nombre,
                ["concepto"] = ConceptoCobro(motivo, sus.Plan.Nombre),
                ["monto"] = SuscripcionBeneficiosHelper.FormatoSoles(transaccion.Monto),
                ["fechaPago"] = SuscripcionBeneficiosHelper.FormatoFechaPeru(now),
                ["vigenteHasta"] = vigenteHasta,
            });
        }

        private static void IniciarPeriodoPagado(Suscripcion sus, DateTime now)
        {
            sus.Estado = TipoEstadoSuscripcion.Activa;
            sus.PeriodoInicio = now;
            sus.PeriodoFin = now.AddMonths(SuscripcionBeneficiosHelper.MesesPorPeriodo(sus.Plan.Periodo));
            sus.AvisoVencimientoEnviadoEn = null;
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
