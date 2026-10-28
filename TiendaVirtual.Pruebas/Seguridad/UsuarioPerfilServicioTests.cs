using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;
using TiendaVirtual.Dominio.Servicios.SeguridadXqm.Implementacion;
using Xunit;

namespace TiendaVirtual.Pruebas.Seguridad
{
    public class UsuarioPerfilServicioTests
    {
        [Fact]
        public async Task SolicitarBaja_SuspendeCuentaYRevocaRefresh()
        {
            var opciones = new DbContextOptionsBuilder<TiendaVirtualDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            await using var contexto = new TiendaVirtualDbContext(opciones);

            var persona = new Persona
            {
                PersonaId = Guid.NewGuid(),
                TipoDocumento = TipoDocumentoIdentidad.DNI,
                NumeroDocumento = "87654321",
                Nombres = "Baja",
                ApellidoPaterno = "Prueba",
            };
            var usuario = new Usuario
            {
                UsuarioId = Guid.NewGuid(),
                PersonaId = persona.PersonaId,
                Correo = "baja@example.com",
                Contrasena = "hash",
                Estado = TipoEstadoUsuario.Activo,
                FechaAlta = DateTime.UtcNow,
            };
            contexto.Personas.Add(persona);
            contexto.Usuarios.Add(usuario);
            contexto.Roles.Add(new Rol { RolId = (short)TipoRol.Cliente, Nombre = "CLIENTE" });
            contexto.UsuarioRoles.Add(new UsuarioRol
            {
                UsuarioId = usuario.UsuarioId,
                RolId = (short)TipoRol.Cliente,
            });
            contexto.TokensRefresco.Add(new TokenRefresco
            {
                UsuarioId = usuario.UsuarioId,
                TokenHash = "abc",
                ExpiraEn = DateTime.UtcNow.AddDays(7),
                FechaEmision = DateTime.UtcNow,
            });
            await contexto.SaveChangesAsync();

            var servicio = new UsuarioPerfilServicio(contexto, NullLogger<UsuarioPerfilServicio>.Instance);
            var r = await servicio.SolicitarBajaAsync(usuario.UsuarioId);

            Assert.True(r.Exito);
            var recargado = await contexto.Usuarios.AsNoTracking()
                .FirstAsync(u => u.UsuarioId == usuario.UsuarioId);
            Assert.Equal(TipoEstadoUsuario.Suspendido, recargado.Estado);
            Assert.True(await contexto.TokensRefresco.AllAsync(t => t.Revocado));
        }
    }
}
