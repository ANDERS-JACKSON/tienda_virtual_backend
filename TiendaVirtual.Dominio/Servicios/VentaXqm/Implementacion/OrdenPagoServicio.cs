using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.PagoXqm;
using TiendaVirtual.Dominio.Opciones;
using TiendaVirtual.Dominio.Servicios.PagoXqm;
using TiendaVirtual.Dominio.Servicios.PagoXqm.Modelos;
using TiendaVirtual.Dominio.Servicios.SoporteXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Servicios.VentaXqm.Implementacion
{
    public class OrdenPagoServicio : IOrdenPagoServicio
    {
        private readonly TiendaVirtualDbContext _context;
        private readonly ILogger<OrdenPagoServicio> _logger;
        private readonly INotificacionServicio _notificacionServicio;
        private readonly IProveedorPagoFactory _proveedorFactory;
        private readonly IOrdenServicio _ordenServicio;
        private readonly ICarritoServicio _carritoServicio;
        private readonly IEmailServicio _emailServicio;
        private readonly IzipayOpciones _izipay;
        private readonly MercadoPagoOpciones _mercadoPago;
        private readonly IHostEnvironment _env;

        public OrdenPagoServicio(
            TiendaVirtualDbContext context,
            INotificacionServicio notificacionServicio,
            IProveedorPagoFactory proveedorFactory,
            IOrdenServicio ordenServicio,
            ICarritoServicio carritoServicio,
            IEmailServicio emailServicio,
            IOptions<IzipayOpciones> izipay,
            IOptions<MercadoPagoOpciones> mercadoPago,
            IHostEnvironment env,
            ILogger<OrdenPagoServicio> logger)
        {
            _context = context;
            _notificacionServicio = notificacionServicio;
            _proveedorFactory = proveedorFactory;
            _ordenServicio = ordenServicio;
            _carritoServicio = carritoServicio;
            _emailServicio = emailServicio;
            _izipay = izipay.Value;
            _mercadoPago = mercadoPago.Value;
            _env = env;
            _logger = logger;
        }

        public async Task<ResultadoOperacion<ConfiguracionCheckoutPagoDto>> ObtenerConfiguracionCheckoutAsync(
            Guid usuarioId)
        {
            try
            {
                var carrito = await _carritoServicio.ObtenerMiCarritoAsync(usuarioId);
                if (!carrito.Exito || carrito.Datos == null || carrito.Datos.TotalItems <= 0)
                    return ResultadoOperacion<ConfiguracionCheckoutPagoDto>.SetError(
                        "Tu carrito está vacío.");

                var proveedor = _proveedorFactory.ObtenerActivo();
                var prep = await proveedor.PrepararCheckoutAsync(new PreparacionCheckoutSolicitud
                {
                    TransaccionId = Guid.Empty,
                    Monto = carrito.Datos.Total,
                    Moneda = "PEN",
                    Concepto = "Checkout carrito",
                    EmailCliente = string.Empty,
                });

                return ResultadoOperacion<ConfiguracionCheckoutPagoDto>.SetExito(new ConfiguracionCheckoutPagoDto
                {
                    Proveedor = prep.CodigoProveedor,
                    PublicKey = prep.PublicKey,
                    RequiereTokenizacionCliente = prep.RequiereTokenizacionCliente,
                    PermiteConfirmacionDemo = prep.PermiteConfirmacionDemo,
                    MontoEstimado = carrito.Datos.Total,
                    Moneda = "PEN",
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Configuración de pago incompleta en ObtenerConfiguracionCheckoutAsync");
                return ResultadoOperacion<ConfiguracionCheckoutPagoDto>.SetError(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en OrdenPagoServicio.ObtenerConfiguracionCheckoutAsync");
                return ResultadoOperacion<ConfiguracionCheckoutPagoDto>.SetError(
                    "Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        public async Task<ResultadoOperacion<ResultadoCobrarCarritoDto>> CobrarCarritoAsync(
            Guid usuarioId, CobrarCarritoDto dto)
        {
            Guid? ordenIdCreada = null;
            try
            {
                var creada = await _ordenServicio.CrearReservandoParaCobroAsync(
                    usuarioId,
                    new CrearOrdenDto { DireccionId = dto.DireccionId });

                if (!creada.Exito || creada.Datos == null)
                    return ResultadoOperacion<ResultadoCobrarCarritoDto>.SetError(
                        creada.Mensaje ?? "No se pudo preparar el pedido para el cobro.");

                ordenIdCreada = creada.Datos.OrdenId;

                var inicio = await IniciarPagoAsync(usuarioId, new IniciarPagoOrdenDto
                {
                    OrdenId = creada.Datos.OrdenId,
                });

                if (!inicio.Exito || inicio.Datos == null)
                {
                    await _ordenServicio.AnularReservaPendientePagoAsync(usuarioId, creada.Datos.OrdenId);
                    return ResultadoOperacion<ResultadoCobrarCarritoDto>.SetError(
                        inicio.Mensaje ?? "No se pudo iniciar el cobro.");
                }

                // Izipay demo (solo Development): confirma sin tokenización.
                if (dto.ConfirmarDemo &&
                    inicio.Datos.PermiteConfirmacionDemo &&
                    !inicio.Datos.RequiereTokenizacionCliente)
                {
                    var demo = await ConfirmarPagoAsync(new ConfirmarPagoOrdenDto
                    {
                        TransaccionId = inicio.Datos.TransaccionId,
                        TransaccionProveedorId = $"IZIPAY-DEMO-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                        MetodoPago = "TARJETA",
                        RespuestaProveedor = JsonSerializer.Serialize(new
                        {
                            demo = true,
                            proveedor = "IZIPAY",
                            exitosa = true,
                        }),
                        Exitosa = true,
                    }, usuarioId);

                    if (!demo.Exito || demo.Datos == null)
                    {
                        await _ordenServicio.AnularReservaPendientePagoAsync(usuarioId, creada.Datos.OrdenId);
                        return ResultadoOperacion<ResultadoCobrarCarritoDto>.SetError(
                            demo.Mensaje ?? "No se pudo confirmar el pago demo.");
                    }

                    await _ordenServicio.VaciarCarritoTrasCobroAsync(usuarioId);
                    return ResultadoOperacion<ResultadoCobrarCarritoDto>.SetExito(new ResultadoCobrarCarritoDto
                    {
                        OrdenId = creada.Datos.OrdenId,
                        NumeroOrden = creada.Datos.NumeroOrden,
                        TransaccionId = demo.Datos.TransaccionId,
                        Proveedor = demo.Datos.Proveedor,
                        TransaccionProveedorId = demo.Datos.TransaccionProveedorId,
                        Monto = demo.Datos.Monto,
                        MetodoPago = demo.Datos.MetodoPago,
                        MensajeUsuario = "Pago confirmado.",
                        Pagado = true,
                        Pendiente = false,
                    });
                }

                if (string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(dto.PaymentMethodId))
                {
                    await _ordenServicio.AnularReservaPendientePagoAsync(usuarioId, creada.Datos.OrdenId);
                    return ResultadoOperacion<ResultadoCobrarCarritoDto>.SetError(
                        "Falta el token de pago. Completa los datos en el formulario seguro.");
                }

                var procesado = await ProcesarPagoAsync(usuarioId, new ProcesarPagoOrdenDto
                {
                    TransaccionId = inicio.Datos.TransaccionId,
                    Token = dto.Token,
                    PaymentMethodId = dto.PaymentMethodId,
                    Installments = dto.Installments <= 0 ? 1 : dto.Installments,
                    IssuerId = dto.IssuerId,
                    PayerEmail = dto.PayerEmail,
                    IdentificationType = dto.IdentificationType,
                    IdentificationNumber = dto.IdentificationNumber,
                });

                if ((!procesado.Exito && procesado.Datos is not { Pendiente: true }) ||
                    procesado.Datos == null)
                {
                    await _ordenServicio.AnularReservaPendientePagoAsync(usuarioId, creada.Datos.OrdenId);
                    return ResultadoOperacion<ResultadoCobrarCarritoDto>.SetError(
                        procesado.Mensaje ?? procesado.Datos?.MensajeUsuario ??
                        "El pago fue rechazado. Tu carrito se mantiene intacto.");
                }

                // Pagado o pendiente de confirmación (webhook): el pedido ya existe;
                // vaciamos el carrito para no cobrar dos veces.
                await _ordenServicio.VaciarCarritoTrasCobroAsync(usuarioId);

                return ResultadoOperacion<ResultadoCobrarCarritoDto>.SetExito(new ResultadoCobrarCarritoDto
                {
                    OrdenId = creada.Datos.OrdenId,
                    NumeroOrden = creada.Datos.NumeroOrden,
                    TransaccionId = procesado.Datos.TransaccionId,
                    Proveedor = procesado.Datos.Proveedor,
                    TransaccionProveedorId = procesado.Datos.TransaccionProveedorId,
                    Monto = procesado.Datos.Monto,
                    MetodoPago = procesado.Datos.MetodoPago,
                    MensajeUsuario = procesado.Datos.MensajeUsuario,
                    Pagado = procesado.Datos.Pagado,
                    Pendiente = procesado.Datos.Pendiente,
                });
            }
            catch (Exception ex)
            {
                if (ordenIdCreada.HasValue)
                {
                    try
                    {
                        await _ordenServicio.AnularReservaPendientePagoAsync(usuarioId, ordenIdCreada.Value);
                    }
                    catch (Exception anularEx)
                    {
                        _logger.LogError(anularEx,
                            "No se pudo anular la reserva tras error en CobrarCarrito. OrdenId={OrdenId}",
                            ordenIdCreada);
                    }
                }

                _logger.LogError(ex, "Error en OrdenPagoServicio.CobrarCarritoAsync");
                return ResultadoOperacion<ResultadoCobrarCarritoDto>.SetError(
                    "Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        public async Task<ResultadoOperacion<RespuestaInicioPagoOrdenDto>> IniciarPagoAsync(
            Guid usuarioId, IniciarPagoOrdenDto dto)
        {
            await using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                var orden = await _context.Ordenes
                    .FirstOrDefaultAsync(o => o.OrdenId == dto.OrdenId && o.ClienteId == usuarioId);

                if (orden == null)
                    return ResultadoOperacion<RespuestaInicioPagoOrdenDto>.SetError("Orden no encontrada.");

                if (orden.Estado != TipoEstadoOrden.PendientePago)
                    return ResultadoOperacion<RespuestaInicioPagoOrdenDto>.SetError(
                        "Solo se puede pagar una orden en estado PendientePago.");

                var proveedor = _proveedorFactory.ObtenerActivo();

                var transaccionExistente = await _context.Transacciones
                    .Where(t => t.OrdenId == orden.OrdenId &&
                                t.Tipo == TipoTransaccion.PagoOrden &&
                                (t.Estado == TipoEstadoTransaccion.Pendiente ||
                                 t.Estado == TipoEstadoTransaccion.Procesando))
                    .OrderByDescending(t => t.TransaccionId)
                    .FirstOrDefaultAsync();

                Transaccion transaccion;
                if (transaccionExistente != null &&
                    transaccionExistente.Monto == orden.Total &&
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
                        OrdenId = orden.OrdenId,
                        UsuarioId = usuarioId,
                        Proveedor = proveedor.CodigoProveedor,
                        Tipo = TipoTransaccion.PagoOrden,
                        Monto = orden.Total,
                        Estado = TipoEstadoTransaccion.Pendiente,
                        Fecha = DateTime.UtcNow,
                    };
                    _context.Transacciones.Add(transaccion);
                    await _context.SaveChangesAsync();
                }

                var prep = await proveedor.PrepararCheckoutAsync(new PreparacionCheckoutSolicitud
                {
                    TransaccionId = transaccion.TransaccionId,
                    Monto = transaccion.Monto,
                    Moneda = "PEN",
                    Concepto = $"Pedido {orden.NumeroOrden}",
                    EmailCliente = orden.CorreoCliente,
                });

                await trx.CommitAsync();

                return ResultadoOperacion<RespuestaInicioPagoOrdenDto>.SetExito(new RespuestaInicioPagoOrdenDto
                {
                    TransaccionId = transaccion.TransaccionId,
                    Monto = transaccion.Monto,
                    Moneda = "PEN",
                    Concepto = $"Pedido {orden.NumeroOrden}",
                    Proveedor = prep.CodigoProveedor,
                    FormToken = prep.FormToken,
                    PublicKey = prep.PublicKey,
                    RequiereTokenizacionCliente = prep.RequiereTokenizacionCliente,
                    PermiteConfirmacionDemo = prep.PermiteConfirmacionDemo,
                });
            }
            catch (InvalidOperationException ex)
            {
                await trx.RollbackAsync();
                _logger.LogWarning(ex, "Configuración de pago incompleta en IniciarPagoAsync");
                return ResultadoOperacion<RespuestaInicioPagoOrdenDto>.SetError(ex.Message);
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                _logger.LogError(ex, "Error en OrdenPagoServicio.IniciarPagoAsync");
                return ResultadoOperacion<RespuestaInicioPagoOrdenDto>.SetError(
                    "Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        public async Task<ResultadoOperacion<ResultadoProcesarPagoOrdenDto>> ProcesarPagoAsync(
            Guid usuarioId, ProcesarPagoOrdenDto dto)
        {
            try
            {
                var transaccion = await _context.Transacciones
                    .FirstOrDefaultAsync(t => t.TransaccionId == dto.TransaccionId);

                if (transaccion == null)
                    return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError("Transacción no encontrada.");

                if (transaccion.UsuarioId != usuarioId)
                    return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError("No autorizado.");

                if (transaccion.Tipo != TipoTransaccion.PagoOrden)
                    return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError("Tipo de transacción no válido.");

                if (transaccion.Estado == TipoEstadoTransaccion.Completada)
                    return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetExito(MapearProcesar(transaccion, pagado: true));

                if (transaccion.Estado is not (TipoEstadoTransaccion.Pendiente or TipoEstadoTransaccion.Procesando))
                    return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError("Esta transacción ya no puede ser modificada.");

                var orden = await _context.Ordenes
                    .FirstOrDefaultAsync(o => o.OrdenId == transaccion.OrdenId);

                if (orden == null || orden.Estado != TipoEstadoOrden.PendientePago)
                    return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError("La orden no admite cobro.");

                var proveedor = _proveedorFactory.ObtenerPorCodigo(transaccion.Proveedor);

                // Si ya hay un cobro en curso en el proveedor, no crear otro (anti doble cobro).
                if (transaccion.Estado == TipoEstadoTransaccion.Procesando &&
                    !string.IsNullOrWhiteSpace(transaccion.TransaccionProveedorId))
                {
                    var estado = await proveedor.ConsultarPagoAsync(transaccion.TransaccionProveedorId);
                    return await AplicarConsultaYMapearAsync(transaccion, estado, dto.PaymentMethodId);
                }

                if (transaccion.Estado == TipoEstadoTransaccion.Procesando)
                    return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError(
                        "Ya hay un cobro en proceso. Espera unos segundos e intenta de nuevo.");

                var esYape = string.Equals(dto.PaymentMethodId, "yape", StringComparison.OrdinalIgnoreCase);
                if (!esYape &&
                    (string.IsNullOrWhiteSpace(dto.IdentificationType) ||
                     string.IsNullOrWhiteSpace(dto.IdentificationNumber)))
                {
                    return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError(
                        "Para pagar con tarjeta indica tipo y número de documento.");
                }

                var email = string.IsNullOrWhiteSpace(dto.PayerEmail)
                    ? orden.CorreoCliente
                    : dto.PayerEmail;

                // Una sola clave por transacción → reintentos de red no generan otro cobro.
                var idempotencyKey = $"orden-{transaccion.TransaccionId:N}";

                transaccion.Estado = TipoEstadoTransaccion.Procesando;
                await _context.SaveChangesAsync();

                var notificationUrl = ObtenerNotificationUrlMercadoPago(proveedor.CodigoProveedor);

                var resultado = await proveedor.CrearPagoAsync(new SolicitudPagoDto
                {
                    TransaccionId = transaccion.TransaccionId,
                    IdempotencyKey = idempotencyKey,
                    Monto = transaccion.Monto,
                    Moneda = "PEN",
                    Descripcion = $"Pedido {orden.NumeroOrden}",
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
                        "Discrepancia de monto MP. TransaccionId={Id} Esperado={Esperado} Recibido={Recibido}",
                        transaccion.TransaccionId, transaccion.Monto, resultado.MontoConfirmado);

                    if (!string.IsNullOrWhiteSpace(resultado.IdPagoExterno))
                    {
                        var reembolso = await proveedor.ReembolsarAsync(resultado.IdPagoExterno);
                        _logger.LogWarning(
                            "Reembolso por monto mismatch. Pago={Pago} Exito={Exito} Msg={Msg}",
                            resultado.IdPagoExterno, reembolso.Exitoso, reembolso.MensajeError);
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
                        "El monto del pago no coincide con la orden. Contacta a soporte.");
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

                    return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetExito(
                        MapearProcesarDesdeOrdenDto(aplicado.Datos, resultado, pagado: true));
                }

                // Recargar tras posible race
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

                var msg = resultado.Pendiente
                    ? MensajesRechazoMercadoPago.ParaUsuario(
                        resultado.StatusDetail ?? "pending_contingency",
                        resultado.MetodoPago ?? dto.PaymentMethodId)
                    : MensajesRechazoMercadoPago.ParaUsuario(
                        resultado.StatusDetail,
                        resultado.MetodoPago ?? dto.PaymentMethodId);
                var dtoOut = MapearProcesar(
                    transaccion,
                    pagado: false,
                    pendiente: resultado.Pendiente,
                    statusDetail: resultado.StatusDetail,
                    mensaje: msg);

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
                _logger.LogError(ex, "Error en OrdenPagoServicio.ProcesarPagoAsync");
                return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError(
                    "Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        public async Task<ResultadoOperacion<TransaccionOrdenDto>> ConfirmarPagoAsync(
            ConfirmarPagoOrdenDto dto, Guid? usuarioIdSolicitante = null)
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

                if (transaccion.Estado is not (TipoEstadoTransaccion.Pendiente or TipoEstadoTransaccion.Procesando))
                    return ResultadoOperacion<TransaccionOrdenDto>.SetError("Esta transacción ya no puede ser modificada.");

                // Mercado Pago NUNCA se confirma desde el frontend.
                if (string.Equals(transaccion.Proveedor, CodigoProveedorPago.MercadoPago, StringComparison.OrdinalIgnoreCase))
                {
                    return ResultadoOperacion<TransaccionOrdenDto>.SetError(
                        "Los pagos de Mercado Pago se confirman automáticamente. No uses este endpoint.");
                }

                var autorizado = false;
                if (usuarioIdSolicitante.HasValue)
                {
                    if (transaccion.UsuarioId != usuarioIdSolicitante.Value)
                        return ResultadoOperacion<TransaccionOrdenDto>.SetError("No autorizado para confirmar esta transacción.");

                    if (_izipay.PermitirConfirmacionDemo)
                    {
                        if (!_env.IsDevelopment())
                        {
                            _logger.LogCritical(
                                "Izipay:PermitirConfirmacionDemo=true fuera de Development. Bloqueado.");
                            return ResultadoOperacion<TransaccionOrdenDto>.SetError(
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
                    return ResultadoOperacion<TransaccionOrdenDto>.SetError("La respuesta del pago no es válida.");
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
                _logger.LogError(ex, "Error en OrdenPagoServicio.ConfirmarPagoAsync");
                return ResultadoOperacion<TransaccionOrdenDto>.SetError(
                    "Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        public async Task<ResultadoOperacion<TransaccionOrdenDto>> AplicarResultadoProveedorAsync(
            ResultadoPagoVerificadoDto resultado)
        {
            await using var trx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var transaccion = await _context.Transacciones
                    .FirstOrDefaultAsync(t => t.TransaccionId == resultado.TransaccionId);

                if (transaccion == null)
                    return ResultadoOperacion<TransaccionOrdenDto>.SetError("Transacción no encontrada.");

                if (transaccion.Tipo != TipoTransaccion.PagoOrden)
                    return ResultadoOperacion<TransaccionOrdenDto>.SetError("Tipo de transacción no válido.");

                // Idempotencia: ya completada → OK sin reactivar ni renotificar.
                if (transaccion.Estado == TipoEstadoTransaccion.Completada)
                    return ResultadoOperacion<TransaccionOrdenDto>.SetExito(transaccion.ToOrdenDto());

                if (transaccion.Estado is not (TipoEstadoTransaccion.Pendiente or TipoEstadoTransaccion.Procesando))
                    return ResultadoOperacion<TransaccionOrdenDto>.SetError("Esta transacción ya no puede ser modificada.");

                if (Math.Round(resultado.MontoConfirmado, 2) != Math.Round(transaccion.Monto, 2))
                {
                    _logger.LogError(
                        "Webhook/confirmación con monto distinto. TransaccionId={Id} Esperado={E} Recibido={R}",
                        transaccion.TransaccionId, transaccion.Monto, resultado.MontoConfirmado);

                    if (!string.IsNullOrWhiteSpace(resultado.IdPagoExterno) &&
                        (resultado.Exitoso || resultado.Pendiente))
                    {
                        try
                        {
                            var proveedor = _proveedorFactory.ObtenerPorCodigo(transaccion.Proveedor);
                            var reembolso = await proveedor.ReembolsarAsync(resultado.IdPagoExterno);
                            _logger.LogWarning(
                                "Reembolso por monto mismatch (webhook). Pago={Pago} Exito={Exito}",
                                resultado.IdPagoExterno, reembolso.Exitoso);
                        }
                        catch (Exception refundEx)
                        {
                            _logger.LogError(refundEx, "No se pudo reembolsar pago con monto mismatch");
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

                    if (transaccion.OrdenId.HasValue)
                    {
                        await _ordenServicio.AnularReservaPendientePagoAsync(
                            transaccion.UsuarioId, transaccion.OrdenId.Value);
                    }

                    return ResultadoOperacion<TransaccionOrdenDto>.SetError(
                        "Discrepancia de monto; pago no aplicado.");
                }

                if (resultado.Pendiente && !resultado.Exitoso)
                {
                    transaccion.Estado = TipoEstadoTransaccion.Procesando;
                    transaccion.TransaccionProveedorId = resultado.IdPagoExterno;
                    transaccion.MetodoPago = resultado.MetodoPago;
                    transaccion.RespuestaProveedor = resultado.RespuestaCruda;
                    transaccion.Fecha = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    await trx.CommitAsync();
                    return ResultadoOperacion<TransaccionOrdenDto>.SetExito(transaccion.ToOrdenDto());
                }

                transaccion.TransaccionProveedorId = resultado.IdPagoExterno;
                transaccion.MetodoPago = resultado.MetodoPago;
                transaccion.RespuestaProveedor = resultado.RespuestaCruda;
                transaccion.Estado = resultado.Exitoso
                    ? TipoEstadoTransaccion.Completada
                    : TipoEstadoTransaccion.Fallida;
                transaccion.Fecha = DateTime.UtcNow;

                string? tituloCliente = null;
                string? cuerpoCliente = null;
                var pagoRechazado = !resultado.Exitoso && !resultado.Pendiente;

                if (resultado.Exitoso && transaccion.OrdenId.HasValue)
                {
                    (tituloCliente, cuerpoCliente, _) =
                        await ActivarOrdenTrasPagoAsync(transaccion.OrdenId.Value);
                }

                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                // Pendiente → rechazado (webhook): liberar stock y cancelar orden.
                if (pagoRechazado && transaccion.OrdenId.HasValue)
                {
                    await _ordenServicio.AnularReservaPendientePagoAsync(
                        transaccion.UsuarioId, transaccion.OrdenId.Value);

                    await _notificacionServicio.CrearAsync(
                        transaccion.UsuarioId,
                        TipoNotificacion.OrdenCanceladaAdmin,
                        "Pago no acreditado",
                        "Tu pago no fue aprobado. El pedido se canceló y el stock quedó libre. Puedes volver a comprar cuando quieras.",
                        new { ordenId = transaccion.OrdenId });
                }

                if (tituloCliente != null && cuerpoCliente != null && transaccion.OrdenId.HasValue)
                {
                    await _notificacionServicio.CrearAsync(
                        transaccion.UsuarioId,
                        TipoNotificacion.OrdenPagada,
                        tituloCliente,
                        cuerpoCliente,
                        new { ordenId = transaccion.OrdenId });

                    await NotificarVendedoresYCorreosTrasPagoAsync(transaccion.OrdenId.Value);
                }

                return ResultadoOperacion<TransaccionOrdenDto>.SetExito(transaccion.ToOrdenDto());
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
                    ? ResultadoOperacion<TransaccionOrdenDto>.SetExito(actual.ToOrdenDto())
                    : ResultadoOperacion<TransaccionOrdenDto>.SetError(
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
                    ? ResultadoOperacion<TransaccionOrdenDto>.SetExito(actual.ToOrdenDto())
                    : ResultadoOperacion<TransaccionOrdenDto>.SetError(
                        "No se pudo confirmar el pago. Intenta de nuevo.");
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                _logger.LogError(ex, "Error en OrdenPagoServicio.AplicarResultadoProveedorAsync");
                return ResultadoOperacion<TransaccionOrdenDto>.SetError(
                    "Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        private static bool EsConflictoDeConcurrencia(DbUpdateException ex)
        {
            // PostgreSQL: 40001 = serialization_failure, 40P01 = deadlock_detected
            return ex.InnerException is PostgresException pgEx &&
                   (pgEx.SqlState == "40001" || pgEx.SqlState == "40P01");
        }

        public async Task<ResultadoOperacion<ResultadoVerificarPagoOrdenDto>> VerificarPagoOrdenAsync(
            Guid usuarioId, VerificarPagoOrdenDto dto)
        {
            try
            {
                var orden = await _context.Ordenes
                    .FirstOrDefaultAsync(o => o.OrdenId == dto.OrdenId && o.ClienteId == usuarioId);

                if (orden == null)
                    return ResultadoOperacion<ResultadoVerificarPagoOrdenDto>.SetError("Orden no encontrada.");

                var proveedor = _proveedorFactory.ObtenerActivo();
                var prep = await proveedor.PrepararCheckoutAsync(new PreparacionCheckoutSolicitud
                {
                    TransaccionId = Guid.Empty,
                    Monto = orden.Total,
                    Moneda = "PEN",
                    Concepto = $"Pedido {orden.NumeroOrden}",
                    EmailCliente = orden.CorreoCliente ?? string.Empty,
                });

                ResultadoVerificarPagoOrdenDto BaseDto(
                    bool pagado,
                    bool pendiente,
                    bool puedeReintentar,
                    string mensaje,
                    Guid? txId = null) => new()
                {
                    OrdenId = orden.OrdenId,
                    NumeroOrden = orden.NumeroOrden,
                    Pagado = pagado,
                    Pendiente = pendiente,
                    PuedeReintentarCobro = puedeReintentar,
                    MensajeUsuario = mensaje,
                    TransaccionId = txId,
                    Monto = orden.Total,
                    Proveedor = prep.CodigoProveedor,
                    PublicKey = prep.PublicKey,
                    RequiereTokenizacionCliente = prep.RequiereTokenizacionCliente,
                    PermiteConfirmacionDemo = prep.PermiteConfirmacionDemo,
                    Moneda = "PEN",
                };

                if (orden.Estado == TipoEstadoOrden.Pagada ||
                    orden.Estado == TipoEstadoOrden.EnPreparacion ||
                    orden.Estado == TipoEstadoOrden.EnCamino ||
                    orden.Estado == TipoEstadoOrden.Entregada)
                {
                    return ResultadoOperacion<ResultadoVerificarPagoOrdenDto>.SetExito(
                        BaseDto(true, false, false, "El pago de este pedido ya está confirmado."));
                }

                if (orden.Estado != TipoEstadoOrden.PendientePago)
                    return ResultadoOperacion<ResultadoVerificarPagoOrdenDto>.SetError(
                        "Este pedido no admite cobro.");

                var transaccion = await _context.Transacciones
                    .Where(t => t.OrdenId == orden.OrdenId &&
                                t.Tipo == TipoTransaccion.PagoOrden &&
                                (t.Estado == TipoEstadoTransaccion.Pendiente ||
                                 t.Estado == TipoEstadoTransaccion.Procesando ||
                                 t.Estado == TipoEstadoTransaccion.Fallida))
                    .OrderByDescending(t => t.TransaccionId)
                    .FirstOrDefaultAsync();

                // Cobro en curso en MP → solo consultar (nunca crear otro).
                if (transaccion is { Estado: TipoEstadoTransaccion.Procesando } &&
                    !string.IsNullOrWhiteSpace(transaccion.TransaccionProveedorId))
                {
                    var estado = await proveedor.ConsultarPagoAsync(transaccion.TransaccionProveedorId);
                    if (estado.ConsultaFallida)
                    {
                        return ResultadoOperacion<ResultadoVerificarPagoOrdenDto>.SetExito(
                            BaseDto(false, true, false,
                                "No pudimos verificar el estado de tu pago en este momento. Intenta de nuevo en unos segundos.",
                                transaccion.TransaccionId));
                    }

                    var aplicado = await AplicarConsultaYMapearAsync(
                        transaccion, estado, transaccion.MetodoPago ?? "yape");

                    await _context.Entry(orden).ReloadAsync();

                    if (aplicado.Datos?.Pagado == true || orden.Estado != TipoEstadoOrden.PendientePago)
                    {
                        return ResultadoOperacion<ResultadoVerificarPagoOrdenDto>.SetExito(
                            BaseDto(true, false, false,
                                "Pago acreditado correctamente.", transaccion.TransaccionId));
                    }

                    if (aplicado.Datos?.Pendiente == true || estado.Pendiente)
                    {
                        // No cancelar aquí: pending_contingency puede demorar (docs MP).
                        // Solo consultar; el webhook o un reintento de verificación actualizan.
                        var msgPendiente = MensajesRechazoMercadoPago.ParaUsuario(
                            estado.StatusDetail,
                            estado.MetodoPago ?? transaccion.MetodoPago);

                        return ResultadoOperacion<ResultadoVerificarPagoOrdenDto>.SetExito(
                            BaseDto(false, true, false, msgPendiente, transaccion.TransaccionId));
                    }

                    // Rechazado tras consulta → la orden puede haberse anulado; recargar.
                    await _context.Entry(orden).ReloadAsync();
                    if (orden.Estado != TipoEstadoOrden.PendientePago)
                    {
                        return ResultadoOperacion<ResultadoVerificarPagoOrdenDto>.SetExito(
                            BaseDto(false, false, false,
                                aplicado.Mensaje ?? "El pago no fue aprobado. El pedido quedó cancelado.",
                                transaccion.TransaccionId));
                    }

                    return ResultadoOperacion<ResultadoVerificarPagoOrdenDto>.SetExito(
                        BaseDto(false, false, true,
                            aplicado.Mensaje ?? "El pago anterior no se completó. Puedes intentar de nuevo.",
                            transaccion.TransaccionId));
                }

                // Procesando sin id externo (fallo de red a mitad): liberar para reintento seguro.
                if (transaccion is { Estado: TipoEstadoTransaccion.Procesando } &&
                    string.IsNullOrWhiteSpace(transaccion.TransaccionProveedorId))
                {
                    transaccion.Estado = TipoEstadoTransaccion.Fallida;
                    transaccion.Fecha = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return ResultadoOperacion<ResultadoVerificarPagoOrdenDto>.SetExito(
                    BaseDto(false, false, true,
                        "Puedes completar el pago de este pedido.",
                        transaccion?.TransaccionId));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Configuración de pago incompleta en VerificarPagoOrdenAsync");
                return ResultadoOperacion<ResultadoVerificarPagoOrdenDto>.SetError(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en OrdenPagoServicio.VerificarPagoOrdenAsync");
                return ResultadoOperacion<ResultadoVerificarPagoOrdenDto>.SetError(
                    "Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        private async Task NotificarVendedoresYCorreosTrasPagoAsync(Guid ordenId)
        {
            try
            {
                var orden = await _context.Ordenes
                    .Include(o => o.Cliente).ThenInclude(c => c.Persona)
                    .Include(o => o.Subordenes).ThenInclude(s => s.Vendedor)
                    .FirstOrDefaultAsync(o => o.OrdenId == ordenId);

                if (orden == null) return;

                var nombreCliente = orden.Cliente?.Persona != null
                    ? $"{orden.Cliente.Persona.Nombres} {orden.Cliente.Persona.ApellidoPaterno}".Trim()
                    : "Cliente";

                var correoCliente = orden.CorreoCliente
                    ?? await _context.Usuarios
                        .Where(u => u.UsuarioId == orden.ClienteId)
                        .Select(u => u.Correo)
                        .FirstOrDefaultAsync();

                if (!string.IsNullOrWhiteSpace(correoCliente))
                {
                    var asunto = $"Pedido {orden.NumeroOrden} confirmado";
                    var cuerpo =
                        $"<p>Hola {System.Net.WebUtility.HtmlEncode(nombreCliente)},</p>" +
                        $"<p>Confirmamos el pago de tu pedido <strong>{System.Net.WebUtility.HtmlEncode(orden.NumeroOrden)}</strong> " +
                        $"por <strong>S/ {orden.Total:N2}</strong>.</p>" +
                        "<p>Los artesanos ya pueden prepararlo. Puedes ver el detalle en Mis pedidos.</p>" +
                        "<p>Gracias por comprar en Artesanías.</p>";

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailServicio.EnviarHtmlAsync(
                                correoCliente!, nombreCliente, asunto, cuerpo);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error enviando correo de pedido pagado al cliente");
                        }
                    });
                }

                foreach (var sub in orden.Subordenes.Where(s => s.Estado == TipoEstadoSuborden.EnPreparacion))
                {
                    var vendedor = sub.Vendedor;
                    if (vendedor == null) continue;

                    await _notificacionServicio.CrearAsync(
                        vendedor.UsuarioId,
                        TipoNotificacion.SubordenEnPreparacion,
                        "Pedido pagado — preparar envío",
                        $"El pedido {sub.NumeroSuborden} fue pagado. Puedes comenzar a prepararlo.",
                        new { subordenId = sub.SubordenId, numeroSuborden = sub.NumeroSuborden },
                        plantillaEmail: PlantillaCorreo.NuevoPedidoVendedor,
                        placeholdersEmail: new Dictionary<string, string>
                        {
                            ["vendedor"] = vendedor.NombreTienda ?? "Artesano",
                            ["numeroPedido"] = sub.NumeroSuborden,
                            ["nombreCliente"] = nombreCliente,
                            ["totalPedido"] = sub.Subtotal.ToString("N2"),
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en NotificarVendedoresYCorreosTrasPagoAsync OrdenId={OrdenId}", ordenId);
            }
        }

        private async Task<ResultadoOperacion<ResultadoProcesarPagoOrdenDto>> AplicarConsultaYMapearAsync(
            Transaccion transaccion,
            EstadoPagoDto estado,
            string paymentMethodIdFallback)
        {
            // Timeout/5xx al consultar ≠ rechazo. No tocar transacción/orden.
            if (estado.ConsultaFallida)
            {
                return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError(
                    "No pudimos verificar el estado de tu pago en este momento. Intenta de nuevo en unos segundos.");
            }

            var aplicado = await AplicarResultadoProveedorAsync(new ResultadoPagoVerificadoDto
            {
                TransaccionId = transaccion.TransaccionId,
                IdPagoExterno = estado.IdPagoExterno ?? transaccion.TransaccionProveedorId ?? string.Empty,
                Exitoso = estado.Exitoso,
                Pendiente = estado.Pendiente,
                MontoConfirmado = estado.MontoConfirmado ?? transaccion.Monto,
                MetodoPago = estado.MetodoPago ?? paymentMethodIdFallback,
                EstadoExterno = estado.EstadoExterno,
                StatusDetail = estado.StatusDetail,
                RespuestaCruda = estado.RespuestaCruda,
            });

            if (!aplicado.Exito || aplicado.Datos == null)
                return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError(
                    aplicado.Mensaje ?? "No se pudo consultar el estado del pago.");

            await _context.Entry(transaccion).ReloadAsync();
            var msg = estado.Exitoso
                ? "Pago acreditado correctamente."
                : estado.Pendiente
                    ? "Pago en proceso de confirmación."
                    : MensajesRechazoMercadoPago.ParaUsuario(
                        estado.StatusDetail,
                        estado.MetodoPago ?? paymentMethodIdFallback);

            if (estado.Exitoso || estado.Pendiente)
            {
                return ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetExito(
                    MapearProcesar(transaccion, estado.Exitoso, estado.Pendiente, estado.StatusDetail, msg));
            }

            return new ResultadoOperacion<ResultadoProcesarPagoOrdenDto>
            {
                Exito = false,
                Mensaje = msg,
                Datos = MapearProcesar(transaccion, false, false, estado.StatusDetail, msg),
            };
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

        private async Task<(string? TituloCliente, string? CuerpoCliente, List<(Guid, string, string)> NotifsVendedor)>
            ActivarOrdenTrasPagoAsync(Guid ordenId)
        {
            var orden = await _context.Ordenes
                .Include(o => o.Subordenes)
                .ThenInclude(s => s.Vendedor)
                .FirstAsync(o => o.OrdenId == ordenId);

            if (orden.Estado != TipoEstadoOrden.PendientePago)
                return (null, null, new List<(Guid, string, string)>());

            orden.Estado = TipoEstadoOrden.Pagada;

            var notifsVendedor = new List<(Guid, string, string)>();
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

        private static ResultadoProcesarPagoOrdenDto MapearProcesar(
            Transaccion t,
            bool pagado,
            bool pendiente = false,
            string? statusDetail = null,
            string? mensaje = null) =>
            new()
            {
                TransaccionId = t.TransaccionId,
                OrdenId = t.OrdenId,
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

        private static ResultadoProcesarPagoOrdenDto MapearProcesarDesdeOrdenDto(
            TransaccionOrdenDto t,
            ResultadoPagoDto r,
            bool pagado) =>
            new()
            {
                TransaccionId = t.TransaccionId,
                OrdenId = t.OrdenId,
                Proveedor = t.Proveedor,
                TransaccionProveedorId = t.TransaccionProveedorId,
                Estado = t.Estado,
                Monto = t.Monto,
                MetodoPago = t.MetodoPago,
                EstadoExterno = r.EstadoExterno,
                StatusDetail = r.StatusDetail,
                MensajeUsuario = pagado ? "Pago acreditado correctamente." : null,
                Pagado = pagado,
                Pendiente = false,
            };
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
                    Nombre = entidad.Tipo.GetDescription(),
                },
                Monto = entidad.Monto,
                Estado = new EnumeracionDto
                {
                    Id = (int)entidad.Estado,
                    Nombre = entidad.Estado.GetDescription(),
                },
                MetodoPago = entidad.MetodoPago,
                Fecha = entidad.Fecha,
            };
    }
}
