using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Dominio.Extensiones;
using TiendaVirtual.Dominio.Extensiones.SeguridadXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;

namespace TiendaVirtual.Dominio.Servicios.SeguridadXqm.Implementacion
{
    public class UsuarioPerfilServicio : IUsuarioPerfilServicio
    {
        private readonly TiendaVirtualDbContext _context;

        public UsuarioPerfilServicio(TiendaVirtualDbContext context)
        {
            _context = context;
        }

        public async Task<ResultadoOperacion<UsuarioPerfilDto>> ObtenerMiPerfilAsync(int usuarioId)
        {
            try
            {
                var usuario = await _context.Usuarios
                    .AsNoTracking()
                    .Include(u => u.Persona)
                    .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);

                if (usuario == null)
                    return ResultadoOperacion<UsuarioPerfilDto>.SetError("Usuario no encontrado.");

                return ResultadoOperacion<UsuarioPerfilDto>.SetExito(MapPerfil(usuario));
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<UsuarioPerfilDto>.SetError("Error al obtener el perfil: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<UsuarioPerfilDto>> ActualizarMisDatosAsync(
            int usuarioId, ActualizarMisDatosPersonalesDto dto)
        {
            try
            {
                if (dto == null)
                    return ResultadoOperacion<UsuarioPerfilDto>.SetError("Datos requeridos.");

                var usuario = await _context.Usuarios
                    .Include(u => u.Persona)
                    .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);

                if (usuario == null)
                    return ResultadoOperacion<UsuarioPerfilDto>.SetError("Usuario no encontrado.");

                var persona = usuario.Persona;
                persona.Nombres = dto.Nombres.Normalizar();
                persona.ApellidoPaterno = dto.ApellidoPaterno?.Normalizar_null();
                persona.ApellidoMaterno = dto.ApellidoMaterno?.Normalizar_null();
                persona.Telefono = string.IsNullOrWhiteSpace(dto.Telefono)
                    ? null
                    : dto.Telefono.Trim();

                var validacion = persona.Validar();
                if (!validacion.Exito)
                    return ResultadoOperacion<UsuarioPerfilDto>.SetError(validacion.Mensaje ?? "Datos inválidos.");

                await _context.SaveChangesAsync();
                return ResultadoOperacion<UsuarioPerfilDto>.SetExito(MapPerfil(usuario));
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<UsuarioPerfilDto>.SetError("Error al actualizar el perfil: " + ex.Message);
            }
        }

        private static UsuarioPerfilDto MapPerfil(Modelo.SeguridadXqm.Usuario usuario) =>
            new()
            {
                UsuarioId = usuario.UsuarioId,
                Correo = usuario.Correo,
                Persona = usuario.Persona.ToDto(),
            };
    }
}
