using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TiendaVirtual.Intercambio;

namespace TiendaVirtual.Dominio.Servicios.SeguridadXqm.Implementacion
{
    public class GoogleAuthServicio : IGoogleAuthServicio
    {
        private readonly IReadOnlyList<string> _clientIds;
        private readonly bool _isDevelopment;

        public GoogleAuthServicio(IConfiguration configuration, IHostEnvironment environment)
        {
            _isDevelopment = environment.IsDevelopment();
            _clientIds = ObtenerClientIds(configuration);
        }

        public async Task<ResultadoOperacion<GoogleTokenPayload>> ValidarIdTokenAsync(string idToken)
        {
            try
            {
                if (_clientIds.Count == 0)
                    return ResultadoOperacion<GoogleTokenPayload>.SetError(
                        "El inicio de sesión con Google no está configurado en el servidor (Google:ClientId).");

                if (string.IsNullOrWhiteSpace(idToken))
                    return ResultadoOperacion<GoogleTokenPayload>.SetError("Token de Google inválido.");

                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = _clientIds
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken.Trim(), settings);

                if (string.IsNullOrWhiteSpace(payload.Subject))
                    return ResultadoOperacion<GoogleTokenPayload>.SetError("Token de Google inválido.");

                if (string.IsNullOrWhiteSpace(payload.Email))
                    return ResultadoOperacion<GoogleTokenPayload>.SetError(
                        "Tu cuenta de Google no tiene correo asociado.");

                return ResultadoOperacion<GoogleTokenPayload>.SetExito(new GoogleTokenPayload
                {
                    Subject = payload.Subject,
                    Email = payload.Email.Trim().ToLowerInvariant(),
                    EmailVerified = payload.EmailVerified,
                    GivenName = payload.GivenName,
                    FamilyName = payload.FamilyName,
                    Name = payload.Name
                });
            }
            catch (InvalidJwtException ex)
            {
                var mensaje = _isDevelopment
                    ? $"Token de Google inválido: {ex.Message}. Verifica que Google:ClientId en el backend coincida con VITE_GOOGLE_CLIENT_ID del frontend."
                    : "El token de Google es inválido o expiró. Intenta de nuevo.";

                return ResultadoOperacion<GoogleTokenPayload>.SetError(mensaje);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<GoogleTokenPayload>.SetError(
                    "No se pudo validar el token de Google: " + ex.Message);
            }
        }

        private static IReadOnlyList<string> ObtenerClientIds(IConfiguration configuration)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);

            var principal = configuration["Google:ClientId"]?.Trim();
            if (EsClientIdValido(principal))
                ids.Add(principal!);

            foreach (var child in configuration.GetSection("Google:ClientIds").GetChildren())
            {
                var normalizado = child.Value?.Trim();
                if (EsClientIdValido(normalizado))
                    ids.Add(normalizado!);
            }

            return ids.ToList();
        }

        private static bool EsClientIdValido(string? clientId) =>
            !string.IsNullOrWhiteSpace(clientId) &&
            !clientId.Contains("TU_CLIENT_ID", StringComparison.OrdinalIgnoreCase);
    }
}
