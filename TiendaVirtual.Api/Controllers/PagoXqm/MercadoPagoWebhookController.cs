using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio;
using TiendaVirtual.Dominio.Servicios.PagoXqm;
using TiendaVirtual.Dominio.Servicios.PagoXqm.Modelos;
using TiendaVirtual.Dominio.Servicios.SuscripcionXqm;
using TiendaVirtual.Dominio.Servicios.VentaXqm;
using Microsoft.EntityFrameworkCore;

namespace TiendaVirtual.Api.Controllers.PagoXqm
{
    /// <summary>
    /// Webhook de Mercado Pago. Autenticidad = firma HMAC, verdad del pago = GET a la API.
    /// </summary>
    [ApiController]
    [Route("api/pagos/mercadopago")]
    [AllowAnonymous]
    public class MercadoPagoWebhookController : ControllerBase
    {
        private readonly IProveedorPagoFactory _factory;
        private readonly IOrdenPagoServicio _ordenPago;
        private readonly ISuscripcionPagoServicio _suscripcionPago;
        private readonly TiendaVirtualDbContext _db;
        private readonly ILogger<MercadoPagoWebhookController> _logger;

        public MercadoPagoWebhookController(
            IProveedorPagoFactory factory,
            IOrdenPagoServicio ordenPago,
            ISuscripcionPagoServicio suscripcionPago,
            TiendaVirtualDbContext db,
            ILogger<MercadoPagoWebhookController> logger)
        {
            _factory = factory;
            _ordenPago = ordenPago;
            _suscripcionPago = suscripcionPago;
            _db = db;
            _logger = logger;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Recibir(CancellationToken cancellationToken)
        {
            string body;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
                body = await reader.ReadToEndAsync(cancellationToken);

            var headers = Request.Headers.ToDictionary(
                h => h.Key,
                h => h.Value.ToString(),
                StringComparer.OrdinalIgnoreCase);

            var query = Request.Query.ToDictionary(
                q => q.Key,
                q => q.Value.ToString(),
                StringComparer.OrdinalIgnoreCase);

            var type = ObtenerTipo(query, body);
            var dataId = ObtenerDataId(query, body);

            _logger.LogInformation(
                "Webhook MP recibido. Type={Type} DataId={DataId}",
                type, dataId);

            // Eventos que no nos interesan: 200 para evitar reintentos.
            if (!EsEventoPago(type))
                return Ok(new { received = true, ignored = true });

            if (string.IsNullOrWhiteSpace(dataId))
            {
                _logger.LogWarning("Webhook MP sin data.id");
                return Ok(new { received = true, ignored = true });
            }

            var mp = _factory.ObtenerPorCodigo(CodigoProveedorPago.MercadoPago);
            var firmaOk = await mp.ValidarFirmaWebhookAsync(headers, query, body, cancellationToken);
            if (!firmaOk)
            {
                _logger.LogWarning("Webhook MP firma inválida. DataId={DataId}", dataId);
                return Unauthorized();
            }

            var estado = await mp.ConsultarPagoAsync(dataId, cancellationToken);

            // Timeout/5xx al CONSULTAR ≠ rechazo. Pedimos reintento a MP sin tocar dominio.
            if (estado.ConsultaFallida)
            {
                _logger.LogWarning(
                    "Webhook MP: no se pudo consultar el pago, se pedirá reintento. DataId={DataId}",
                    dataId);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }

            if (string.IsNullOrWhiteSpace(estado.ExternalReference) ||
                !Guid.TryParse(estado.ExternalReference, out var transaccionId))
            {
                _logger.LogWarning(
                    "Webhook MP sin external_reference válida. DataId={DataId} Ref={Ref}",
                    dataId, estado.ExternalReference);
                return Ok(new { received = true, unmatched = true });
            }

            var transaccion = await _db.Transacciones.AsNoTracking()
                .FirstOrDefaultAsync(t => t.TransaccionId == transaccionId, cancellationToken);

            if (transaccion == null)
            {
                _logger.LogWarning(
                    "Webhook MP: transacción no encontrada {TransaccionId}",
                    transaccionId);
                return Ok(new { received = true, unmatched = true });
            }

            var verificado = new ResultadoPagoVerificadoDto
            {
                TransaccionId = transaccionId,
                IdPagoExterno = estado.IdPagoExterno ?? dataId,
                Exitoso = estado.Exitoso,
                Pendiente = estado.Pendiente,
                // Nunca fallback a 0: un monto nulo no debe disparar monto_mismatch/reembolso.
                MontoConfirmado = estado.MontoConfirmado ?? transaccion.Monto,
                MetodoPago = estado.MetodoPago,
                EstadoExterno = estado.EstadoExterno,
                StatusDetail = estado.StatusDetail,
                RespuestaCruda = estado.RespuestaCruda,
            };

            if (transaccion.Tipo == TipoTransaccion.PagoOrden)
            {
                var r = await _ordenPago.AplicarResultadoProveedorAsync(verificado);
                _logger.LogInformation(
                    "Webhook MP orden aplicado. TransaccionId={Id} Exito={Exito} Msg={Msg}",
                    transaccionId, r.Exito, r.Mensaje);
            }
            else if (transaccion.Tipo == TipoTransaccion.PagoSuscripcion)
            {
                var r = await _suscripcionPago.AplicarResultadoProveedorAsync(verificado);
                _logger.LogInformation(
                    "Webhook MP suscripción aplicado. TransaccionId={Id} Exito={Exito} Msg={Msg}",
                    transaccionId, r.Exito, r.Mensaje);
            }
            else
            {
                _logger.LogWarning("Webhook MP tipo no soportado {Tipo}", transaccion.Tipo);
            }

            return Ok(new { received = true });
        }

        private static bool EsEventoPago(string? type)
        {
            if (string.IsNullOrWhiteSpace(type)) return true; // query topic a veces no viene
            type = type.ToLowerInvariant();
            return type is "payment" or "payments" or "topic_payment";
        }

        private static string? ObtenerTipo(IReadOnlyDictionary<string, string> query, string body)
        {
            if (query.TryGetValue("type", out var t) && !string.IsNullOrWhiteSpace(t))
                return t;
            if (query.TryGetValue("topic", out var topic) && !string.IsNullOrWhiteSpace(topic))
                return topic;

            try
            {
                using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
                if (doc.RootElement.TryGetProperty("type", out var typeProp))
                    return typeProp.GetString();
                if (doc.RootElement.TryGetProperty("action", out var actionProp))
                    return actionProp.GetString();
            }
            catch
            {
                /* ignore */
            }

            return null;
        }

        private static string? ObtenerDataId(IReadOnlyDictionary<string, string> query, string body)
        {
            if (query.TryGetValue("data.id", out var id) && !string.IsNullOrWhiteSpace(id))
                return id;
            if (query.TryGetValue("id", out var id2) && !string.IsNullOrWhiteSpace(id2))
                return id2;

            try
            {
                using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
                if (doc.RootElement.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("id", out var dataId))
                {
                    return dataId.ValueKind == JsonValueKind.Number
                        ? dataId.GetInt64().ToString()
                        : dataId.GetString();
                }
            }
            catch
            {
                /* ignore */
            }

            return null;
        }
    }
}
