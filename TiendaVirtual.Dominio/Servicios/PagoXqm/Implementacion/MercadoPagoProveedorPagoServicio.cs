using MercadoPago.Client;
using MercadoPago.Client.Payment;
using MercadoPago.Config;
using MercadoPago.Error;
using MercadoPago.Resource.Payment;
using MercadoPago.Webhook;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Opciones;
using TiendaVirtual.Dominio.Servicios.PagoXqm.Modelos;

namespace TiendaVirtual.Dominio.Servicios.PagoXqm.Implementacion
{
    public sealed class MercadoPagoProveedorPagoServicio : IProveedorPagoServicio
    {
        private readonly MercadoPagoOpciones _opciones;
        private readonly ILogger<MercadoPagoProveedorPagoServicio> _logger;
        private readonly object _configLock = new();
        private string? _tokenConfigurado;

        public MercadoPagoProveedorPagoServicio(
            IOptions<MercadoPagoOpciones> opciones,
            ILogger<MercadoPagoProveedorPagoServicio> logger)
        {
            _opciones = opciones.Value;
            _logger = logger;
        }

        public string CodigoProveedor => CodigoProveedorPago.MercadoPago;

        public Task<PreparacionCheckoutResultado> PrepararCheckoutAsync(
            PreparacionCheckoutSolicitud solicitud,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_opciones.PublicKey))
            {
                throw new InvalidOperationException(
                    "MercadoPago:PublicKey no configurada. Use user-secrets o variables de entorno.");
            }

            ValidarParCredenciales();

            return Task.FromResult(new PreparacionCheckoutResultado
            {
                CodigoProveedor = CodigoProveedor,
                PublicKey = _opciones.PublicKey,
                FormToken = null,
                RequiereTokenizacionCliente = true,
                PermiteConfirmacionDemo = false,
            });
        }

        public async Task<ResultadoPagoDto> CrearPagoAsync(
            SolicitudPagoDto solicitud,
            CancellationToken cancellationToken = default)
        {
            AsegurarAccessToken();
            ValidarParCredenciales();

            if (string.IsNullOrWhiteSpace(solicitud.EmailPagador))
            {
                return new ResultadoPagoDto
                {
                    Exitoso = false,
                    MensajeError = "Falta el correo del pagador (requerido por Mercado Pago).",
                };
            }

            if (string.IsNullOrWhiteSpace(solicitud.Token))
            {
                return new ResultadoPagoDto
                {
                    Exitoso = false,
                    MensajeError = "Falta el token de pago (nunca envíe datos de tarjeta al servidor).",
                };
            }

            // Defensa: rechazar payloads que parezcan PAN en claro.
            if (PareceNumeroTarjeta(solicitud.Token))
            {
                _logger.LogError(
                    "Intento de enviar posible PAN en claro. TransaccionId={TransaccionId}",
                    solicitud.TransaccionId);
                return new ResultadoPagoDto
                {
                    Exitoso = false,
                    MensajeError = "Formato de token inválido.",
                };
            }

            try
            {
                var client = new PaymentClient();
                var esYape = string.Equals(solicitud.PaymentMethodId, "yape", StringComparison.OrdinalIgnoreCase);
                // Tienda sin cuotas: siempre 1.
                // binary_mode SOLO en tarjeta: fuerza approved/rejected.
                // En Yape NO usarlo — docs MP: si el cobro iría a pending/in_process,
                // binary_mode lo convierte en rejected (p. ej. cc_rejected_other_reason)
                // y el usuario ve un falso "banco rechazó / prueba otra tarjeta".
                var request = new PaymentCreateRequest
                {
                    TransactionAmount = solicitud.Monto,
                    Token = solicitud.Token,
                    Description = solicitud.Descripcion,
                    Installments = 1,
                    PaymentMethodId = solicitud.PaymentMethodId,
                    BinaryMode = !esYape,
                    ExternalReference = solicitud.TransaccionId.ToString("D"),
                    Payer = new PaymentPayerRequest
                    {
                        Email = solicitud.EmailPagador,
                    },
                };

                // No enviar localhost: MP no puede notificar ahí y a veces rechaza el create.
                if (!string.IsNullOrWhiteSpace(solicitud.NotificationUrl) &&
                    !solicitud.NotificationUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase) &&
                    !solicitud.NotificationUrl.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))
                {
                    request.NotificationUrl = solicitud.NotificationUrl;
                }

                if (!string.IsNullOrWhiteSpace(solicitud.IssuerId))
                    request.IssuerId = solicitud.IssuerId;

                if (!string.IsNullOrWhiteSpace(solicitud.IdentificationType) &&
                    !string.IsNullOrWhiteSpace(solicitud.IdentificationNumber))
                {
                    request.Payer.Identification = new MercadoPago.Client.Common.IdentificationRequest
                    {
                        Type = solicitud.IdentificationType,
                        Number = solicitud.IdentificationNumber,
                    };
                }

                var options = new RequestOptions
                {
                    // Por si CustomHeaders no estuviera inicializado en alguna versión.
                    AccessToken = _opciones.AccessToken,
                };
                options.CustomHeaders["X-Idempotency-Key"] = solicitud.IdempotencyKey;

                Payment payment = await client.CreateAsync(request, options, cancellationToken);
                var mapeado = MapearResultado(payment);

                if (esYape && mapeado.Pendiente)
                {
                    _logger.LogWarning(
                        "Yape devolvió estado pendiente (inesperado según docs MP). Status={Status} Detail={Detail} PaymentId={Id}",
                        payment.Status, payment.StatusDetail, payment.Id);
                }

                return mapeado;
            }
            catch (MercadoPagoApiException ex)
            {
                // Errores 4xx/5xx de la API (antes caían en Exception genérica y se ocultaban).
                _logger.LogWarning(
                    ex,
                    "MercadoPago API rechazó CrearPago. TransaccionId={TransaccionId} Status={Status}",
                    solicitud.TransaccionId, ex.StatusCode);

                var detalle = ex.ApiError?.Message;
                if (string.IsNullOrWhiteSpace(detalle))
                    detalle = ex.Message;

                return new ResultadoPagoDto
                {
                    Exitoso = false,
                    Pendiente = false,
                    MensajeError = MensajeAmigableApi(detalle, ex.StatusCode),
                    StatusDetail = ex.ApiError?.Error,
                    RespuestaCruda = SerializarSeguro(new
                    {
                        status = ex.StatusCode,
                        apiMessage = ex.ApiError?.Message,
                        error = ex.ApiError?.Error,
                        message = ex.Message,
                    }),
                };
            }
            catch (MercadoPagoException ex)
            {
                _logger.LogWarning(
                    ex,
                    "MercadoPago CrearPago falló. TransaccionId={TransaccionId}",
                    solicitud.TransaccionId);

                return new ResultadoPagoDto
                {
                    Exitoso = false,
                    Pendiente = false,
                    MensajeError = "No se pudo procesar el pago con Mercado Pago.",
                    RespuestaCruda = SerializarSeguro(new { error = ex.Message }),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en MercadoPago.CrearPagoAsync");
                return new ResultadoPagoDto
                {
                    Exitoso = false,
                    MensajeError =
                        $"Error al procesar el pago: {ex.GetType().Name}: {ex.Message}",
                };
            }
        }

        public async Task<EstadoPagoDto> ConsultarPagoAsync(
            string idPagoExterno,
            CancellationToken cancellationToken = default)
        {
            AsegurarAccessToken();

            if (!long.TryParse(idPagoExterno, NumberStyles.Integer, CultureInfo.InvariantCulture, out var paymentId))
            {
                return new EstadoPagoDto
                {
                    IdPagoExterno = idPagoExterno,
                    Exitoso = false,
                    Pendiente = false,
                    ConsultaFallida = false, // id inválido: rechazo real, no error transitorio
                    EstadoExterno = "invalid_id",
                };
            }

            try
            {
                var client = new PaymentClient();
                var options = new RequestOptions { AccessToken = _opciones.AccessToken };
                Payment payment = await client.GetAsync(paymentId, options, cancellationToken);
                var r = MapearResultado(payment);

                return new EstadoPagoDto
                {
                    IdPagoExterno = r.IdPagoExterno,
                    EstadoExterno = r.EstadoExterno,
                    StatusDetail = r.StatusDetail,
                    Exitoso = r.Exitoso,
                    Pendiente = r.Pendiente,
                    ConsultaFallida = false,
                    MontoConfirmado = r.MontoConfirmado,
                    ExternalReference = payment.ExternalReference,
                    MetodoPago = r.MetodoPago,
                    RespuestaCruda = r.RespuestaCruda,
                };
            }
            catch (Exception ex)
            {
                // Timeout/5xx al CONSULTAR ≠ pago rechazado. No cancelar pedidos ni liberar stock.
                _logger.LogError(ex, "Error consultando pago MP {PaymentId}", idPagoExterno);
                return new EstadoPagoDto
                {
                    IdPagoExterno = idPagoExterno,
                    Exitoso = false,
                    Pendiente = false,
                    ConsultaFallida = true,
                    EstadoExterno = "consulta_fallida",
                };
            }
        }

        public Task<bool> ValidarFirmaWebhookAsync(
            IReadOnlyDictionary<string, string> headers,
            IReadOnlyDictionary<string, string> query,
            string cuerpoCrudo,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_opciones.WebhookSecret))
            {
                _logger.LogError("MercadoPago:WebhookSecret no configurado; rechazando webhook.");
                return Task.FromResult(false);
            }

            var xSignature = ObtenerHeader(headers, "x-signature");
            var xRequestId = ObtenerHeader(headers, "x-request-id");
            var dataId = ObtenerQuery(query, "data.id") ?? ObtenerQuery(query, "id");

            try
            {
                WebhookSignatureValidator.Validate(
                    xSignature: xSignature,
                    xRequestId: xRequestId,
                    dataId: dataId,
                    secret: _opciones.WebhookSecret);

                return Task.FromResult(true);
            }
            catch (InvalidWebhookSignatureException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Webhook MP firma inválida. RequestId={RequestId}",
                    xRequestId);
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando firma webhook MP");
                return Task.FromResult(false);
            }
        }

        public async Task<bool> CancelarPagoAsync(
            string idPagoExterno,
            CancellationToken cancellationToken = default)
        {
            AsegurarAccessToken();

            if (!long.TryParse(idPagoExterno, NumberStyles.Integer, CultureInfo.InvariantCulture, out var paymentId))
                return false;

            try
            {
                var client = new PaymentClient();
                var options = new RequestOptions { AccessToken = _opciones.AccessToken };
                options.CustomHeaders["X-Idempotency-Key"] = $"cancel-{paymentId}";
                Payment cancelled = await client.CancelAsync(paymentId, options, cancellationToken);
                var status = (cancelled.Status ?? string.Empty).ToLowerInvariant();
                _logger.LogInformation(
                    "Cancelación MP PaymentId={Id} Status={Status}",
                    paymentId, cancelled.Status);
                return status is "cancelled" or "rejected";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo cancelar pago MP {PaymentId}", idPagoExterno);
                return false;
            }
        }

        public async Task<ResultadoReembolsoDto> ReembolsarAsync(
            string idPagoExterno,
            decimal? montoParcial = null,
            CancellationToken cancellationToken = default)
        {
            AsegurarAccessToken();

            if (!long.TryParse(idPagoExterno, NumberStyles.Integer, CultureInfo.InvariantCulture, out var paymentId))
            {
                return new ResultadoReembolsoDto
                {
                    Exitoso = false,
                    MensajeError = "Id de pago externo inválido.",
                };
            }

            try
            {
                var client = new PaymentClient();
                var options = new RequestOptions { AccessToken = _opciones.AccessToken };
                options.CustomHeaders["X-Idempotency-Key"] =
                    $"refund-{paymentId}-{(montoParcial?.ToString("F2", CultureInfo.InvariantCulture) ?? "full")}";

                PaymentRefund refund = montoParcial.HasValue
                    ? await client.RefundAsync(paymentId, montoParcial.Value, options, cancellationToken)
                    : await client.RefundAsync(paymentId, options, cancellationToken);

                return new ResultadoReembolsoDto
                {
                    Exitoso = string.Equals(refund.Status, "approved", StringComparison.OrdinalIgnoreCase),
                    IdReembolsoExterno = refund.Id?.ToString(CultureInfo.InvariantCulture),
                    MontoReembolsado = refund.Amount,
                    EstadoExterno = refund.Status,
                    RespuestaCruda = SerializarSeguro(new
                    {
                        refund.Id,
                        refund.Status,
                        refund.Amount,
                        refund.PaymentId,
                    }),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reembolsando pago MP {PaymentId}", idPagoExterno);
                return new ResultadoReembolsoDto
                {
                    Exitoso = false,
                    MensajeError = "No se pudo procesar el reembolso en Mercado Pago.",
                };
            }
        }

        private void AsegurarAccessToken()
        {
            if (string.IsNullOrWhiteSpace(_opciones.AccessToken))
            {
                throw new InvalidOperationException(
                    "MercadoPago:AccessToken no configurado. Use user-secrets o variables de entorno.");
            }

            lock (_configLock)
            {
                if (_tokenConfigurado == _opciones.AccessToken)
                    return;

                MercadoPagoConfig.AccessToken = _opciones.AccessToken;
                _tokenConfigurado = _opciones.AccessToken;
            }
        }

        /// <summary>
        /// Public Key y Access Token deben ser del mismo modo (ambas TEST- o ambas APP_USR-/producción).
        /// </summary>
        private void ValidarParCredenciales()
        {
            var pk = _opciones.PublicKey ?? string.Empty;
            var at = _opciones.AccessToken ?? string.Empty;
            var pkTest = pk.StartsWith("TEST-", StringComparison.OrdinalIgnoreCase);
            var atTest = at.StartsWith("TEST-", StringComparison.OrdinalIgnoreCase);
            var pkProd = pk.StartsWith("APP_USR-", StringComparison.OrdinalIgnoreCase);
            var atProd = at.StartsWith("APP_USR-", StringComparison.OrdinalIgnoreCase);

            if ((pkTest && atProd) || (pkProd && atTest))
            {
                throw new InvalidOperationException(
                    "MercadoPago: Public Key y Access Token son de ambientes distintos (TEST vs producción). Usa el par de la misma aplicación en Tus integraciones.");
            }

            if (pkTest != atTest && pkProd != atProd)
            {
                _logger.LogWarning(
                    "MercadoPago: no se pudo clasificar el par PublicKey/AccessToken. Verifica que ambas sean del mismo ambiente.");
            }
        }

        private static ResultadoPagoDto MapearResultado(Payment payment)
        {
            var status = (payment.Status ?? string.Empty).ToLowerInvariant();
            var exitoso = status == "approved";
            var pendiente = status is "pending" or "in_process" or "authorized";

            return new ResultadoPagoDto
            {
                Exitoso = exitoso,
                Pendiente = pendiente && !exitoso,
                IdPagoExterno = payment.Id?.ToString(CultureInfo.InvariantCulture),
                EstadoExterno = payment.Status,
                StatusDetail = payment.StatusDetail,
                MontoConfirmado = payment.TransactionAmount,
                MetodoPago = payment.PaymentMethodId,
                RespuestaCruda = SerializarSeguro(new
                {
                    payment.Id,
                    payment.Status,
                    payment.StatusDetail,
                    payment.TransactionAmount,
                    payment.PaymentMethodId,
                    payment.ExternalReference,
                    payment.DateApproved,
                }),
            };
        }

        private static string MensajeAmigableApi(string? detalle, int? statusCode)
        {
            var raw = (detalle ?? string.Empty).ToLowerInvariant();

            if (statusCode == 401 || raw.Contains("unauthorized") || raw.Contains("invalid access token"))
                return "Credenciales de Mercado Pago inválidas. Verifica Access Token y Public Key.";

            if (raw.Contains("invalid_card_token") || raw.Contains("invalid token") || raw.Contains("card_token"))
                return "El token de tarjeta expiró o es inválido. Vuelve a ingresar los datos e intenta de nuevo.";

            if (raw.Contains("issuer") || raw.Contains("bin"))
                return "No se pudo identificar el banco emisor. Verifica el número de tarjeta.";

            if (raw.Contains("payer.email") || raw.Contains("email"))
                return "El correo del pagador no es válido.";

            if (raw.Contains("identification") || raw.Contains("documento"))
                return "Tipo o número de documento inválido. Verifica los datos del pagador.";

            if (!string.IsNullOrWhiteSpace(detalle) && detalle.Length <= 220)
                return detalle;

            return "Mercado Pago rechazó el cobro. Verifica los datos de la tarjeta e intenta de nuevo.";
        }

        private static bool PareceNumeroTarjeta(string token)
        {
            var digits = new string(token.Where(char.IsDigit).ToArray());
            return digits.Length >= 13 && digits.Length <= 19 && token.All(c => char.IsDigit(c) || char.IsWhiteSpace(c) || c == '-');
        }

        private static string? ObtenerHeader(IReadOnlyDictionary<string, string> headers, string name)
        {
            foreach (var kv in headers)
            {
                if (string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            }

            return null;
        }

        private static string? ObtenerQuery(IReadOnlyDictionary<string, string> query, string name)
        {
            foreach (var kv in query)
            {
                if (string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            }

            return null;
        }

        private static string SerializarSeguro(object? value)
        {
            try
            {
                return JsonSerializer.Serialize(value);
            }
            catch
            {
                return "{}";
            }
        }
    }
}
