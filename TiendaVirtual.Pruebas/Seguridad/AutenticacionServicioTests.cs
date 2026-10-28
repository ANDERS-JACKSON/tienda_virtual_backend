using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;
using TiendaVirtual.Dominio.Servicios.SeguridadXqm.Implementacion;
using TiendaVirtual.Dominio.Servicios.Sistema;
using TiendaVirtual.Dominio.Servicios.Sistema.Implementacion;
using TiendaVirtual.Dominio.Utilidad;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;
using TiendaVirtual.Pruebas.Fakes;
using Xunit;

namespace TiendaVirtual.Pruebas.Seguridad
{
    public class AutenticacionServicioTests
    {
        private static (AutenticacionServicio Servicio, TiendaVirtualDbContext Contexto) CrearServicio()
        {
            var opciones = new DbContextOptionsBuilder<TiendaVirtualDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                // El proveedor InMemory no soporta transacciones reales; algunos
                // métodos del servicio (p. ej. RegistrarClienteAsync) abren una
                // transacción explícita, así que silenciamos esa advertencia aquí.
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            var contexto = new TiendaVirtualDbContext(opciones);

            var configuracion = ConfiguracionDePrueba.Crear();
            var jwtService = new JwtTokenService(configuracion);
            ITwoFactorService twoFactorService = new TwoFactorService();

            var servicio = new AutenticacionServicio(
                contexto,
                jwtService,
                twoFactorService,
                configuracion,
                new NotificacionServicioFalso(),
                new GoogleAuthServicioFalso(),
                new EmailServicioFalso(),
                NullLogger<AutenticacionServicio>.Instance,
                new AuditoriaServicioFalso());

            return (servicio, contexto);
        }

        private static Usuario CrearUsuarioCliente(
            TiendaVirtualDbContext contexto, string correo, string contrasenaPlana)
        {
            var persona = new Persona
            {
                PersonaId = Guid.NewGuid(),
                TipoDocumento = TipoDocumentoIdentidad.DNI,
                NumeroDocumento = "12345678",
                Nombres = "Prueba",
                ApellidoPaterno = "Uno",
                ApellidoMaterno = "Dos",
            };
            contexto.Personas.Add(persona);

            var usuario = new Usuario
            {
                UsuarioId = Guid.NewGuid(),
                PersonaId = persona.PersonaId,
                Correo = correo,
                Contrasena = BCrypt.Net.BCrypt.HashPassword(contrasenaPlana),
                CorreoConfirmado = true,
                Estado = TipoEstadoUsuario.Activo,
                FechaAlta = DateTime.UtcNow,
            };
            contexto.Usuarios.Add(usuario);

            if (!contexto.Roles.Any(r => r.RolId == (short)TipoRol.Cliente))
                contexto.Roles.Add(new Rol { RolId = (short)TipoRol.Cliente, Nombre = "CLIENTE" });

            contexto.UsuarioRoles.Add(new UsuarioRol
            {
                UsuarioId = usuario.UsuarioId,
                RolId = (short)TipoRol.Cliente,
            });

            contexto.SaveChanges();
            return usuario;
        }

        // ─────────────────────────────────────────────────────────────
        // A4+M12: bloqueo de cuenta tras intentos fallidos de login.
        // ─────────────────────────────────────────────────────────────
        [Fact]
        public async Task Login_BloqueaLaCuentaTrasCincoIntentosFallidos()
        {
            var (servicio, contexto) = CrearServicio();
            var usuario = CrearUsuarioCliente(contexto, "cliente@example.com", "ClaveCorrecta123!");

            for (var i = 0; i < 5; i++)
            {
                var r = await servicio.LoginAsync(
                    new LoginDto { Correo = usuario.Correo, Contrasena = "clave-equivocada" },
                    "127.0.0.1", "pruebas");
                Assert.False(r.Exito);
            }

            // La cuenta debe quedar bloqueada aunque ahora se use la contraseña correcta.
            var intentoConClaveCorrecta = await servicio.LoginAsync(
                new LoginDto { Correo = usuario.Correo, Contrasena = "ClaveCorrecta123!" },
                "127.0.0.1", "pruebas");

            Assert.False(intentoConClaveCorrecta.Exito);
            Assert.Contains("Demasiados intentos", intentoConClaveCorrecta.Mensaje, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Login_ConClaveCorrecta_ReiniciaElContadorDeIntentos()
        {
            var (servicio, contexto) = CrearServicio();
            var usuario = CrearUsuarioCliente(contexto, "cliente2@example.com", "ClaveCorrecta123!");

            await servicio.LoginAsync(
                new LoginDto { Correo = usuario.Correo, Contrasena = "mala" }, "127.0.0.1", "pruebas");
            await servicio.LoginAsync(
                new LoginDto { Correo = usuario.Correo, Contrasena = "mala" }, "127.0.0.1", "pruebas");

            var exito = await servicio.LoginAsync(
                new LoginDto { Correo = usuario.Correo, Contrasena = "ClaveCorrecta123!" },
                "127.0.0.1", "pruebas");

            Assert.True(exito.Exito);

            var usuarioActualizado = await contexto.Usuarios.FirstAsync(u => u.UsuarioId == usuario.UsuarioId);
            Assert.Equal(0, usuarioActualizado.IntentosFallidosAcceso);
            Assert.Null(usuarioActualizado.BloqueadoHasta);
        }

        // ─────────────────────────────────────────────────────────────
        // C2: establecer-contrasena-inicial — mensajes genéricos, vencimiento
        // y bloqueo por intentos.
        // ─────────────────────────────────────────────────────────────
        [Fact]
        public async Task EstablecerContrasenaInicial_RechazaClaveTemporalVencida()
        {
            var (servicio, contexto) = CrearServicio();
            var usuario = CrearUsuarioCliente(contexto, "nuevo@example.com", "123456");
            usuario.ForzarCambioClave = true;
            usuario.ClaveTemporalExpira = DateTime.UtcNow.AddHours(-1); // ya venció
            await contexto.SaveChangesAsync();

            var resultado = await servicio.EstablecerContrasenaInicialAsync(
                usuario.Correo, "123456", "NuevaClaveSegura123!");

            Assert.False(resultado.Exito);
            Assert.Contains("no es correcto o ya venció", resultado.Mensaje);
        }

        [Fact]
        public async Task EstablecerContrasenaInicial_MismoMensajeParaCorreoInexistenteYCodigoIncorrecto()
        {
            var (servicio, contexto) = CrearServicio();
            var usuario = CrearUsuarioCliente(contexto, "existe@example.com", "654321");
            usuario.ForzarCambioClave = true;
            usuario.ClaveTemporalExpira = DateTime.UtcNow.AddHours(24);
            await contexto.SaveChangesAsync();

            var correoInexistente = await servicio.EstablecerContrasenaInicialAsync(
                "no-existe@example.com", "654321", "NuevaClaveSegura123!");

            var codigoIncorrecto = await servicio.EstablecerContrasenaInicialAsync(
                usuario.Correo, "000000", "NuevaClaveSegura123!");

            Assert.False(correoInexistente.Exito);
            Assert.False(codigoIncorrecto.Exito);
            // M10: ambos casos devuelven exactamente el mismo mensaje —
            // no se puede distinguir "no existe" de "código incorrecto".
            Assert.Equal(correoInexistente.Mensaje, codigoIncorrecto.Mensaje);
        }

        [Fact]
        public async Task EstablecerContrasenaInicial_ConCodigoCorrecto_FuncionaYRevocaSesiones()
        {
            var (servicio, contexto) = CrearServicio();
            var usuario = CrearUsuarioCliente(contexto, "activa@example.com", "999999");
            usuario.ForzarCambioClave = true;
            usuario.ClaveTemporalExpira = DateTime.UtcNow.AddHours(24);
            contexto.TokensRefresco.Add(new TokenRefresco
            {
                UsuarioId = usuario.UsuarioId,
                TokenHash = "hash-de-sesion-vieja",
                ExpiraEn = DateTime.UtcNow.AddDays(30),
                Revocado = false,
                FechaEmision = DateTime.UtcNow,
            });
            await contexto.SaveChangesAsync();

            var resultado = await servicio.EstablecerContrasenaInicialAsync(
                usuario.Correo, "999999", "NuevaClaveSegura123!");

            Assert.True(resultado.Exito);

            var usuarioActualizado = await contexto.Usuarios.FirstAsync(u => u.UsuarioId == usuario.UsuarioId);
            Assert.False(usuarioActualizado.ForzarCambioClave);
            Assert.Null(usuarioActualizado.ClaveTemporalExpira);

            var sesionVieja = await contexto.TokensRefresco.FirstAsync(t => t.UsuarioId == usuario.UsuarioId);
            Assert.True(sesionVieja.Revocado);
        }

        // ─────────────────────────────────────────────────────────────
        // B2: logout sólo puede revocar refresh tokens del propio usuario.
        // ─────────────────────────────────────────────────────────────
        [Fact]
        public async Task Logout_NoPermiteRevocarElRefreshTokenDeOtroUsuario()
        {
            var (servicio, contexto) = CrearServicio();
            var victima = CrearUsuarioCliente(contexto, "victima@example.com", "clave123");
            var atacante = CrearUsuarioCliente(contexto, "atacante@example.com", "clave123");

            const string tokenPlano = "refresh-token-de-la-victima";
            var jwtService = new JwtTokenService(ConfiguracionDePrueba.Crear());
            var hash = jwtService.HashearRefreshToken(tokenPlano);

            contexto.TokensRefresco.Add(new TokenRefresco
            {
                UsuarioId = victima.UsuarioId,
                TokenHash = hash,
                ExpiraEn = DateTime.UtcNow.AddDays(30),
                Revocado = false,
                FechaEmision = DateTime.UtcNow,
            });
            await contexto.SaveChangesAsync();

            // El atacante (autenticado como sí mismo) intenta revocar el refresh
            // token de la víctima adivinando/copiando el valor.
            var resultado = await servicio.LogoutAsync(atacante.UsuarioId, tokenPlano);
            Assert.True(resultado.Exito); // respuesta idempotente, no revela nada

            var tokenEnBd = await contexto.TokensRefresco.FirstAsync(t => t.UsuarioId == victima.UsuarioId);
            Assert.False(tokenEnBd.Revocado); // la sesión de la víctima sigue viva
        }

        [Fact]
        public async Task Logout_SiRevocaSuPropioToken()
        {
            var (servicio, contexto) = CrearServicio();
            var usuario = CrearUsuarioCliente(contexto, "dueno@example.com", "clave123");

            const string tokenPlano = "refresh-token-propio";
            var jwtService = new JwtTokenService(ConfiguracionDePrueba.Crear());
            var hash = jwtService.HashearRefreshToken(tokenPlano);

            contexto.TokensRefresco.Add(new TokenRefresco
            {
                UsuarioId = usuario.UsuarioId,
                TokenHash = hash,
                ExpiraEn = DateTime.UtcNow.AddDays(30),
                Revocado = false,
                FechaEmision = DateTime.UtcNow,
            });
            await contexto.SaveChangesAsync();

            var resultado = await servicio.LogoutAsync(usuario.UsuarioId, tokenPlano);
            Assert.True(resultado.Exito);

            var tokenEnBd = await contexto.TokensRefresco.FirstAsync(t => t.UsuarioId == usuario.UsuarioId);
            Assert.True(tokenEnBd.Revocado);
        }

        // ─────────────────────────────────────────────────────────────
        // Honeypot anti-bot en Registro (cliente/vendedor) y compra como
        // invitado: a pedido del negocio estos flujos NO muestran captcha,
        // así que el campo oculto "Sitio" es la defensa anti-bot. Si llega
        // lleno, se rechaza sin crear nada.
        // ─────────────────────────────────────────────────────────────
        private static TiendaVirtual.Intercambio.Dto.SeguridadXqm.PersonaDto CrearPersonaDtoDePrueba(string correo) => new()
        {
            TipoDocumento = new TiendaVirtual.Intercambio.Dto.Sistema.EnumeracionDto((int)TipoDocumentoIdentidad.DNI, "DNI"),
            NumeroDocumento = "87654321",
            Nombres = "Bot",
            ApellidoPaterno = "De Prueba",
            ApellidoMaterno = "Honeypot",
            CorreoElectronico = correo,
        };

        [Fact]
        public async Task RegistrarCliente_ConHoneypotLleno_NoCreaNadaYDevuelveError()
        {
            var (servicio, contexto) = CrearServicio();
            var correo = "bot-cliente@example.com";

            var resultado = await servicio.RegistrarClienteAsync(
                new RegistroClienteDto
                {
                    Correo = correo,
                    Persona = CrearPersonaDtoDePrueba(correo),
                    AceptaTerminos = true,
                    Sitio = "https://spam.example.com", // un bot llenó el campo oculto
                },
                "127.0.0.1", "bot");

            Assert.False(resultado.Exito);
            Assert.False(await contexto.Usuarios.AnyAsync(u => u.Correo == correo));
            Assert.False(await contexto.Personas.AnyAsync(p => p.NumeroDocumento == "87654321"));
        }

        [Fact]
        public async Task RegistrarVendedor_ConHoneypotLleno_NoCreaNadaYDevuelveError()
        {
            var (servicio, contexto) = CrearServicio();
            var correo = "bot-vendedor@example.com";

            var resultado = await servicio.RegistrarVendedorAsync(
                new RegistroVendedorDto
                {
                    Correo = correo,
                    Persona = CrearPersonaDtoDePrueba(correo),
                    NombreTienda = "Tienda Bot",
                    SlugTienda = "tienda-bot",
                    AceptaTerminos = true,
                    Sitio = "relleno-de-bot",
                },
                "127.0.0.1", "bot");

            Assert.False(resultado.Exito);
            Assert.False(await contexto.Usuarios.AnyAsync(u => u.Correo == correo));
            Assert.False(await contexto.Vendedores.AnyAsync(v => v.SlugTienda == "tienda-bot"));
        }

        [Fact]
        public async Task PrepararCompraInvitado_ConHoneypotLleno_NoEnviaCodigoYDevuelveError()
        {
            var (servicio, contexto) = CrearServicio();
            var correo = "bot-invitado@example.com";

            var resultado = await servicio.PrepararCompraInvitadoAsync(
                new PrepararCompraInvitadoDto
                {
                    Correo = correo,
                    Persona = CrearPersonaDtoDePrueba(correo),
                    AceptaTerminos = true,
                    Sitio = "relleno-de-bot",
                },
                "127.0.0.1", "bot");

            Assert.False(resultado.Exito);
            Assert.False(await contexto.Usuarios.AnyAsync(u => u.Correo == correo));
        }

        [Fact]
        public async Task RegistrarCliente_SinHoneypot_FuncionaNormal()
        {
            var (servicio, contexto) = CrearServicio();
            var correo = "persona-real@example.com";

            var resultado = await servicio.RegistrarClienteAsync(
                new RegistroClienteDto
                {
                    Correo = correo,
                    Persona = CrearPersonaDtoDePrueba(correo),
                    AceptaTerminos = true,
                    Sitio = null, // una persona real deja el campo oculto vacío
                },
                "127.0.0.1", "navegador-real");

            Assert.True(resultado.Exito);
            Assert.True(await contexto.Usuarios.AnyAsync(u => u.Correo == correo));
            var creado = await contexto.Usuarios.FirstAsync(u => u.Correo == correo);
            Assert.Equal(PoliticasConsentimiento.VersionVigente, creado.VersionPoliticas);
        }

        [Fact]
        public async Task LoginAdmin_SinCorreoConfirmado_NoEntregaQrDe2Fa()
        {
            var (servicio, contexto) = CrearServicio();
            var clave = "ClaveAdmin123!";
            var usuario = CrearUsuarioCliente(contexto, "admin-nuevo@example.com", clave);
            usuario.CorreoConfirmado = false;

            if (!contexto.Roles.Any(r => r.RolId == (short)TipoRol.Admin))
                contexto.Roles.Add(new Rol { RolId = (short)TipoRol.Admin, Nombre = "ADMIN" });
            contexto.UsuarioRoles.Add(new UsuarioRol
            {
                UsuarioId = usuario.UsuarioId,
                RolId = (short)TipoRol.Admin,
            });
            await contexto.SaveChangesAsync();

            var resultado = await servicio.LoginAsync(
                new LoginDto { Correo = usuario.Correo, Contrasena = clave },
                "127.0.0.1", "prueba");

            Assert.False(resultado.Exito);
            Assert.Contains("correo", resultado.Mensaje, StringComparison.OrdinalIgnoreCase);
        }
    }
}
