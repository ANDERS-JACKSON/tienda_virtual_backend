using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using TiendaVirtual.Dominio.Extensiones.SeguridadXqm;
using TiendaVirtual.Dominio.Servicios.Sistema.Implementacion;
using TiendaVirtual.Dominio.Utilidad;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;
using Xunit;

namespace TiendaVirtual.Pruebas.Seguridad
{
    public class CloudinaryFirmaServiceTests
    {
        private static CloudinaryFirmaService Crear(Dictionary<string, string?> valores)
        {
            var configuracion = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
            return new CloudinaryFirmaService(configuracion);
        }

        [Fact]
        public void SinSecret_ActivoFalse_YNoRompeSubidaUnsigned()
        {
            var servicio = Crear(new Dictionary<string, string?>());

            Assert.False(servicio.Activo);
            var r = servicio.Firmar("imagen", ["VENDEDOR"]);
            Assert.True(r.Exito);
            Assert.False(r.Datos!.Activo);
            Assert.Null(r.Datos.Signature);
        }

        [Fact]
        public void Firma_CoincideConSha1DocumentadoPorCloudinary()
        {
            var parametros = new Dictionary<string, string>
            {
                ["folder"] = "tienda/imagenes",
                ["timestamp"] = "1315060510",
            };
            const string secret = "abcd";

            var firma = CloudinaryFirmaService.FirmarParametros(parametros, secret);

            var esperado = Convert.ToHexString(
                SHA1.HashData(Encoding.UTF8.GetBytes("folder=tienda/imagenes&timestamp=1315060510" + secret)))
                .ToLowerInvariant();

            Assert.Equal(esperado, firma);
            Assert.DoesNotContain(secret, firma);
        }

        [Fact]
        public void ClienteSinRolVendedor_NoFirmaPdfNiKyc()
        {
            var servicio = Crear(new Dictionary<string, string?>
            {
                ["Cloudinary:CloudName"] = "demo",
                ["Cloudinary:ApiKey"] = "123",
                ["Cloudinary:ApiSecret"] = "abcd",
            });

            var pdf = servicio.Firmar("pdf", ["CLIENTE"]);
            var kyc = servicio.Firmar("kyc", ["CLIENTE"]);
            var img = servicio.Firmar("imagen", ["CLIENTE"]);

            Assert.False(pdf.Exito);
            Assert.False(kyc.Exito);
            Assert.True(img.Exito);
            Assert.True(img.Datos!.Activo);
            Assert.Equal("image", img.Datos.ResourceType);
        }
    }

    public class IdentificadorMediaTests
    {
        [Theory]
        [InlineData("tienda/imagenes/abc123")]
        [InlineData("patrones/archivo")]
        [InlineData("https://res.cloudinary.com/demo/image/upload/v1/tienda/abc")]
        public void AceptaPublicIdYUrlCloudinary(string valor)
        {
            Assert.True(IdentificadorMedia.EsValido(valor));
        }

        [Theory]
        [InlineData("javascript:alert(1)")]
        [InlineData("data:image/png;base64,aaaa")]
        [InlineData("https://evil.example/x.png")]
        [InlineData("http://res.cloudinary.com/demo/x")]
        [InlineData("../etc/passwd")]
        [InlineData("abc def")]
        public void RechazaXssYHostsAjenos(string valor)
        {
            Assert.False(IdentificadorMedia.EsValido(valor));
        }

        [Fact]
        public void VacioEsValidoSoloEnOpcional()
        {
            Assert.False(IdentificadorMedia.EsValido(null));
            Assert.True(IdentificadorMedia.EsValidoOVacio(null));
            Assert.True(IdentificadorMedia.EsValidoOVacio(""));
        }
    }

    public class CifradoCampoTests
    {
        private static readonly string Clave =
            Convert.ToBase64String(Encoding.UTF8.GetBytes("12345678901234567890123456789012"));

        [Fact]
        public void CifraYDescifra_YElPlanoLegadoSigueSirviendo()
        {
            var cifrado = CifradoCampo.Cifrar("smtp-secret", Clave);

            Assert.StartsWith(CifradoCampo.Prefijo, cifrado);
            Assert.DoesNotContain("smtp-secret", cifrado);
            Assert.Equal("smtp-secret", CifradoCampo.Descifrar(cifrado, Clave));
            Assert.Equal("plano-viejo", CifradoCampo.Descifrar("plano-viejo", Clave));
        }
    }

    public class PersonaIdServidorTests
    {
        [Fact]
        public void ToEntidad_IgnoraPersonaIdDelCliente()
        {
            var ajeno = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var dto = new PersonaDto
            {
                PersonaId = ajeno,
                TipoDocumento = new EnumeracionDto { Id = 1, Nombre = "DNI" },
                NumeroDocumento = "12345678",
                Nombres = "Ana",
            };

            var entidad = dto.ToEntidad();

            Assert.NotEqual(ajeno, entidad.PersonaId);
            Assert.NotEqual(Guid.Empty, entidad.PersonaId);
        }
    }

    public class UsuarioDtoNoFiltraHashTests
    {
        [Fact]
        public void ToDto_NoCopiaElHashDeContrasena()
        {
            var usuario = new TiendaVirtual.Dominio.Modelo.SeguridadXqm.Usuario
            {
                UsuarioId = Guid.NewGuid(),
                PersonaId = Guid.NewGuid(),
                Correo = "a@b.c",
                Contrasena = "$2a$11$hash-que-no-debe-salir",
                Estado = TiendaVirtual.Comun.Enumeracion.TipoEstadoUsuario.Activo,
                FechaAlta = DateTime.UtcNow,
            };

            var dto = usuario.ToDto();
            Assert.Equal(string.Empty, dto.Contrasena);
        }
    }
}
