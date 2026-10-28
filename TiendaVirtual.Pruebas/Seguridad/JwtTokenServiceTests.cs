using System;
using TiendaVirtual.Dominio.Servicios.Sistema.Implementacion;
using TiendaVirtual.Pruebas.Fakes;
using Xunit;

namespace TiendaVirtual.Pruebas.Seguridad
{
    /// <summary>
    /// A2: el token temporal de 2FA (o el ticket de registro por correo) nunca debe
    /// validar como un token de sesión normal. Usan una audiencia distinta
    /// ("&lt;Audience&gt;.temporal"), así que ValidarToken (el que usa el pipeline
    /// [Authorize]) debe rechazarlos siempre.
    /// </summary>
    public class JwtTokenServiceTests
    {
        private static JwtTokenService CrearServicio() => new(ConfiguracionDePrueba.Crear());

        [Fact]
        public void TokenTemporal2Fa_NoEsValidoComoTokenNormal()
        {
            var servicio = CrearServicio();
            var usuarioId = Guid.NewGuid();

            var tokenTemporal = servicio.GenerarTokenTemporal2Fa(usuarioId);

            // Exactamente el ataque de A2: usar el token de "2FA pendiente" como si
            // fuera un access token normal en un endpoint [Authorize].
            var principalComoNormal = servicio.ValidarToken(tokenTemporal);
            Assert.Null(principalComoNormal);

            // Pero sí debe ser válido a través del canal dedicado a tokens temporales.
            var principalComoTemporal = servicio.ValidarTokenTemporal(tokenTemporal);
            Assert.NotNull(principalComoTemporal);
            Assert.Equal("2fa_pending", principalComoTemporal!.FindFirst("scope")?.Value);
        }

        [Fact]
        public void TicketRegistroCorreo_NoEsValidoComoTokenNormal()
        {
            var servicio = CrearServicio();

            var ticket = servicio.GenerarTicketRegistroCorreo("cliente@example.com");

            var principalComoNormal = servicio.ValidarToken(ticket);
            Assert.Null(principalComoNormal);

            var correoLeido = servicio.LeerCorreoTicketRegistro(ticket);
            Assert.Equal("cliente@example.com", correoLeido);
        }

        [Fact]
        public void TokenNormal_NoEsValidoComoTokenTemporal()
        {
            var servicio = CrearServicio();

            var (token, _) = servicio.GenerarToken(
                Guid.NewGuid(), "cliente@example.com", Guid.NewGuid(), new() { "CLIENTE" });

            // Un token de sesión normal tampoco debería colarse por el canal temporal
            // (defensa simétrica, aunque hoy nada lo intente).
            var principal = servicio.ValidarTokenTemporal(token);
            Assert.Null(principal);

            // Pero sí es válido por el canal normal.
            Assert.NotNull(servicio.ValidarToken(token));
        }

        [Fact]
        public void TokenConAlgNone_EsRechazado()
        {
            var servicio = CrearServicio();
            var unsigned = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: "pruebas.tiendavirtual",
                audience: "pruebas.tiendavirtual.clientes",
                claims: new[] { new System.Security.Claims.Claim("sub", Guid.NewGuid().ToString()) },
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(10));

            var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(unsigned);

            Assert.Null(servicio.ValidarToken(jwt));
        }
    }
}
