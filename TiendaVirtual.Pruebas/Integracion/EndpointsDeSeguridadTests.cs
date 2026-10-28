using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TiendaVirtual.Dominio.Servicios.Sistema.Implementacion;
using Xunit;

namespace TiendaVirtual.Pruebas.Integracion
{
    /// <summary>
    /// Pruebas de extremo a extremo (HTTP real, vía WebApplicationFactory) de los
    /// hallazgos más críticos del Bloque 1.
    /// </summary>
    public class EndpointsDeSeguridadTests : IClassFixture<FabricaDePruebas>
    {
        private readonly FabricaDePruebas _fabrica;

        public EndpointsDeSeguridadTests(FabricaDePruebas fabrica)
        {
            _fabrica = fabrica;
        }

        // ─────────────────────────────────────────────────────────────
        // A1+M1: MetodoEnvioController ya no es público. El ataque exacto
        // reportado era: PUT /api/MetodoEnvio/1 sin token modificaba un
        // método de envío porque [AllowAnonymous] estaba a nivel de clase.
        // ─────────────────────────────────────────────────────────────
        [Fact]
        public async Task Get_MetodoEnvio_EsPublico()
        {
            var cliente = _fabrica.CreateClient();
            var respuesta = await cliente.GetAsync("/api/MetodoEnvio");
            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        }

        [Fact]
        public async Task Put_MetodoEnvio_SinToken_Devuelve401()
        {
            var cliente = _fabrica.CreateClient();
            var respuesta = await cliente.PutAsync(
                "/api/MetodoEnvio/1",
                new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        }

        [Fact]
        public async Task Post_MetodoEnvio_SinToken_Devuelve401()
        {
            var cliente = _fabrica.CreateClient();
            var respuesta = await cliente.PostAsync(
                "/api/MetodoEnvio",
                new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        }

        // ─────────────────────────────────────────────────────────────
        // M1: un endpoint sin [Authorize]/[AllowAnonymous] propio (hipotético,
        // cubierto hoy por cualquier GET fuera de los controladores explícitamente
        // públicos) también debe caer bajo la FallbackPolicy. Lo verificamos con
        // un endpoint real protegido que requiere rol ADMIN.
        // ─────────────────────────────────────────────────────────────
        [Fact]
        public async Task Get_EndpointAdmin_SinToken_Devuelve401()
        {
            var cliente = _fabrica.CreateClient();
            var respuesta = await cliente.GetAsync("/api/Rol");
            Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        }

        // ─────────────────────────────────────────────────────────────
        // A2: el token temporal de 2FA (audiencia "...​.temporal") no debe
        // servir como access token en un endpoint [Authorize] normal.
        // ─────────────────────────────────────────────────────────────
        [Fact]
        public async Task TokenTemporal2Fa_UsadoComoBearer_Devuelve401EnEndpointNormal()
        {
            var configuracion = new ConfigurationBuilder()
                .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "clave-de-pruebas-que-tiene-al-menos-32-bytes-de-longitud",
                    ["Jwt:Issuer"] = "pruebas.tiendavirtual",
                    ["Jwt:Audience"] = "pruebas.tiendavirtual.clientes",
                })
                .Build();
            var jwtService = new JwtTokenService(configuracion);
            var tokenTemporal = jwtService.GenerarTokenTemporal2Fa(System.Guid.NewGuid());

            var cliente = _fabrica.CreateClient();
            cliente.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", tokenTemporal);

            // Endpoint normal protegido con [Authorize] (no es uno de los de 2FA).
            var respuesta = await cliente.GetAsync("/api/Autenticacion/cuenta/seguridad");

            Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        }

        [Fact]
        public async Task Respuesta_IncluyeCabecerasDeSeguridad()
        {
            var cliente = _fabrica.CreateClient();
            var respuesta = await cliente.GetAsync("/api/MetodoEnvio");

            Assert.Equal("nosniff", respuesta.Headers.GetValues("X-Content-Type-Options").Single());
            Assert.Equal("DENY", respuesta.Headers.GetValues("X-Frame-Options").Single());
            Assert.Equal(
                "strict-origin-when-cross-origin",
                respuesta.Headers.GetValues("Referrer-Policy").Single());
        }

        [Fact]
        public async Task Post_MediaFirma_SinToken_Devuelve401()
        {
            var cliente = _fabrica.CreateClient();
            var respuesta = await cliente.PostAsync(
                "/api/media/firma",
                new StringContent("{\"proposito\":\"imagen\"}", System.Text.Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        }
    }
}
