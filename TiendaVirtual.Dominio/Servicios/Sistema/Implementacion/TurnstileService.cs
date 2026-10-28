using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TiendaVirtual.Dominio.Servicios.Sistema.Implementacion
{
    public class TurnstileService : ITurnstileService
    {
        private const string ENDPOINT_VERIFICACION =
            "https://challenges.cloudflare.com/turnstile/v0/siteverify";

        // HttpClient estático: es seguro para llamadas concurrentes y evita el
        // agotamiento de sockets que causaría crear uno por request. No usamos
        // IHttpClientFactory para no sumar el paquete Microsoft.Extensions.Http
        // a un proyecto de dominio que hoy no lo necesita para nada más.
        private static readonly HttpClient ClienteHttp = new() { Timeout = TimeSpan.FromSeconds(5) };

        private readonly IConfiguration _configuration;
        private readonly ILogger<TurnstileService> _logger;

        public TurnstileService(
            IConfiguration configuration,
            ILogger<TurnstileService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public bool Activo =>
            _configuration.GetValue<bool>("Turnstile:Activo")
            && !string.IsNullOrWhiteSpace(_configuration["Turnstile:SecretKey"]);

        public async Task<bool> ValidarAsync(string? token, string? direccionIp)
        {
            if (!Activo)
                return true; // Captcha apagado (desarrollo o antes de configurar Cloudflare).

            if (string.IsNullOrWhiteSpace(token))
                return false;

            try
            {
                var secretKey = _configuration["Turnstile:SecretKey"]!;

                var campos = new Dictionary<string, string>
                {
                    ["secret"] = secretKey,
                    ["response"] = token
                };
                if (!string.IsNullOrWhiteSpace(direccionIp))
                    campos["remoteip"] = direccionIp;

                using var contenido = new FormUrlEncodedContent(campos);
                using var respuesta = await ClienteHttp.PostAsync(ENDPOINT_VERIFICACION, contenido);
                if (!respuesta.IsSuccessStatusCode)
                    return false;

                using var stream = await respuesta.Content.ReadAsStreamAsync();
                using var documento = await JsonDocument.ParseAsync(stream);
                return documento.RootElement.TryGetProperty("success", out var exito)
                       && exito.ValueKind == JsonValueKind.True;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar Turnstile.");
                return false;
            }
        }
    }
}
