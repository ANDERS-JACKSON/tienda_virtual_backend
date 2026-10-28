using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TiendaVirtual.Dominio.Servicios.Sistema.Implementacion;
using Xunit;

namespace TiendaVirtual.Pruebas.Seguridad
{
    /// <summary>
    /// A4+M12: Turnstile debe quedar completamente apagado por defecto (desarrollo)
    /// y sólo exigir el token cuando esté explícitamente activado y configurado.
    /// </summary>
    public class TurnstileServiceTests
    {
        private static TurnstileService Crear(Dictionary<string, string?> valores)
        {
            var configuracion = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
            return new TurnstileService(configuracion, NullLogger<TurnstileService>.Instance);
        }

        [Fact]
        public async Task PorDefecto_SinConfiguracion_NoBloqueaNada()
        {
            var servicio = Crear(new Dictionary<string, string?>());

            Assert.False(servicio.Activo);
            Assert.True(await servicio.ValidarAsync(null, "127.0.0.1"));
            Assert.True(await servicio.ValidarAsync("", "127.0.0.1"));
        }

        [Fact]
        public async Task ActivoSinSecretKey_SigueApagado()
        {
            var servicio = Crear(new Dictionary<string, string?>
            {
                ["Turnstile:Activo"] = "true",
            });

            Assert.False(servicio.Activo); // falta SecretKey -> se considera apagado
            Assert.True(await servicio.ValidarAsync(null, "127.0.0.1"));
        }

        [Fact]
        public async Task ActivoYConfigurado_RechazaTokenVacio()
        {
            var servicio = Crear(new Dictionary<string, string?>
            {
                ["Turnstile:Activo"] = "true",
                ["Turnstile:SecretKey"] = "clave-de-prueba",
            });

            Assert.True(servicio.Activo);
            Assert.False(await servicio.ValidarAsync(null, "127.0.0.1"));
            Assert.False(await servicio.ValidarAsync("", "127.0.0.1"));
        }
    }
}
