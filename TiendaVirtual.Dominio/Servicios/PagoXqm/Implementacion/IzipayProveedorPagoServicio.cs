using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Opciones;
using TiendaVirtual.Dominio.Servicios.PagoXqm.Modelos;

namespace TiendaVirtual.Dominio.Servicios.PagoXqm.Implementacion
{
    /// <summary>
    /// Adaptador Izipay: conserva el modo demo existente. Cobro real vía API Izipay = TODO futuro.
    /// </summary>
    public sealed class IzipayProveedorPagoServicio : IProveedorPagoServicio
    {
        private readonly IzipayOpciones _opciones;
        private readonly IHostEnvironment _env;
        private readonly ILogger<IzipayProveedorPagoServicio> _logger;

        public IzipayProveedorPagoServicio(
            IOptions<IzipayOpciones> opciones,
            IHostEnvironment env,
            ILogger<IzipayProveedorPagoServicio> logger)
        {
            _opciones = opciones.Value;
            _env = env;
            _logger = logger;
        }

        public string CodigoProveedor => CodigoProveedorPago.Izipay;

        public Task<PreparacionCheckoutResultado> PrepararCheckoutAsync(
            PreparacionCheckoutSolicitud solicitud,
            CancellationToken cancellationToken = default)
        {
            var formToken =
                $"DEMO-FORM-TOKEN-{solicitud.TransaccionId:N}-{Guid.NewGuid():N}";

            var permiteDemo = _opciones.PermitirConfirmacionDemo && _env.IsDevelopment();

            return Task.FromResult(new PreparacionCheckoutResultado
            {
                CodigoProveedor = CodigoProveedor,
                PublicKey = string.IsNullOrWhiteSpace(_opciones.PublicKey)
                    ? "DEMO-PUBLIC-KEY"
                    : _opciones.PublicKey,
                FormToken = formToken,
                RequiereTokenizacionCliente = false,
                PermiteConfirmacionDemo = permiteDemo,
            });
        }

        public Task<ResultadoPagoDto> CrearPagoAsync(
            SolicitudPagoDto solicitud,
            CancellationToken cancellationToken = default)
        {
            _logger.LogWarning(
                "Izipay.CrearPagoAsync no implementado (proveedor demo). TransaccionId={TransaccionId}",
                solicitud.TransaccionId);

            return Task.FromResult(new ResultadoPagoDto
            {
                Exitoso = false,
                Pendiente = false,
                MensajeError =
                    "Izipay real aún no está integrado. Usa confirmación demo en Development o cambia a Mercado Pago.",
            });
        }

        public Task<EstadoPagoDto> ConsultarPagoAsync(
            string idPagoExterno,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EstadoPagoDto
            {
                IdPagoExterno = idPagoExterno,
                Exitoso = false,
                Pendiente = false,
                ConsultaFallida = false,
                EstadoExterno = "unknown",
            });
        }

        public Task<bool> ValidarFirmaWebhookAsync(
            IReadOnlyDictionary<string, string> headers,
            IReadOnlyDictionary<string, string> query,
            string cuerpoCrudo,
            CancellationToken cancellationToken = default)
        {
            // Stub: Izipay real validaría kr-hash. Nunca aceptar webhooks sin firma en producción.
            _logger.LogWarning("ValidarFirmaWebhookAsync Izipay no implementado; rechazando.");
            return Task.FromResult(false);
        }

        public Task<ResultadoReembolsoDto> ReembolsarAsync(
            string idPagoExterno,
            decimal? montoParcial = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ResultadoReembolsoDto
            {
                Exitoso = false,
                MensajeError = "Reembolso Izipay no implementado. Use el flujo admin manual.",
                RespuestaCruda = JsonSerializer.Serialize(new { idPagoExterno, montoParcial }),
            });
        }

        public Task<bool> CancelarPagoAsync(
            string idPagoExterno,
            CancellationToken cancellationToken = default)
            => Task.FromResult(false);
    }
}
