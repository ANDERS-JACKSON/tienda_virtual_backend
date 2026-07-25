using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaVirtual.Dominio.Extensiones;
using TiendaVirtual.Dominio.Extensiones.SeguridadXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;

namespace TiendaVirtual.Dominio.Servicios.SeguridadXqm.Implementacion
{
    public class UsuarioPerfilServicio : IUsuarioPerfilServicio
    {
        private readonly TiendaVirtualDbContext _context;
        private readonly ILogger<UsuarioPerfilServicio> _logger;

        public UsuarioPerfilServicio(TiendaVirtualDbContext context, ILogger<UsuarioPerfilServicio> logger)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<ResultadoOperacion<UsuarioPerfilDto>> ObtenerMiPerfilAsync(Guid usuarioId)
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
                _logger.LogError(ex, "Error en UsuarioPerfilServicio.ObtenerMiPerfilAsync");
                return ResultadoOperacion<UsuarioPerfilDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<UsuarioPerfilDto>> ActualizarMisDatosAsync(
            Guid usuarioId, ActualizarMisDatosPersonalesDto dto)
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
                _logger.LogError(ex, "Error en UsuarioPerfilServicio.ActualizarMisDatosAsync");
                return ResultadoOperacion<UsuarioPerfilDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
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
