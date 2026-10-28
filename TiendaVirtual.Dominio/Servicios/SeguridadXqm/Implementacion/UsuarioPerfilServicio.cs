using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaVirtual.Comun.Enumeracion;
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
                if (!Enum.IsDefined(typeof(TipoDocumentoIdentidad), (int)dto.TipoDocumentoId))
                    return ResultadoOperacion<UsuarioPerfilDto>.SetError("Selecciona un tipo de documento válido.");

                var documento = new string((dto.NumeroDocumento ?? string.Empty).Where(char.IsDigit).ToArray());
                persona.Nombres = dto.Nombres.Normalizar();
                persona.ApellidoPaterno = dto.ApellidoPaterno?.Normalizar_null();
                persona.ApellidoMaterno = dto.ApellidoMaterno?.Normalizar_null();
                persona.Telefono = string.IsNullOrWhiteSpace(dto.Telefono)
                    ? null
                    : dto.Telefono.Trim();
                persona.TipoDocumento = (TipoDocumentoIdentidad)dto.TipoDocumentoId;
                persona.NumeroDocumento = documento;

                var validacion = persona.Validar();
                if (!validacion.Exito)
                    return ResultadoOperacion<UsuarioPerfilDto>.SetError(validacion.Mensaje ?? "Datos inválidos.");

                var documentoOcupado = await _context.Personas.AnyAsync(p =>
                    p.PersonaId != persona.PersonaId
                    && p.TipoDocumento == persona.TipoDocumento
                    && p.NumeroDocumento == documento);
                if (documentoOcupado)
                    return ResultadoOperacion<UsuarioPerfilDto>.SetError(
                        "Ya hay una cuenta registrada con este documento.");

                await _context.SaveChangesAsync();
                return ResultadoOperacion<UsuarioPerfilDto>.SetExito(MapPerfil(usuario));
            }
            catch (DbUpdateException)
            {
                return ResultadoOperacion<UsuarioPerfilDto>.SetError(
                    "Ya hay una cuenta registrada con este documento.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en UsuarioPerfilServicio.ActualizarMisDatosAsync");
                return ResultadoOperacion<UsuarioPerfilDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        public async Task<ResultadoOperacion<bool>> SolicitarBajaAsync(Guid usuarioId)
        {
            try
            {
                var usuario = await _context.Usuarios
                    .Include(u => u.UsuarioRoles).ThenInclude(ur => ur.Rol)
                    .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);

                if (usuario == null)
                    return ResultadoOperacion<bool>.SetError("Usuario no encontrado.");

                if (usuario.UsuarioRoles.Any(ur => ur.Rol.Nombre == "ADMIN"))
                {
                    var totalAdmins = await _context.UsuarioRoles
                        .CountAsync(ur => ur.Rol.Nombre == "ADMIN" &&
                            _context.Usuarios.Any(uu => uu.UsuarioId == ur.UsuarioId &&
                                uu.Estado == TipoEstadoUsuario.Activo));
                    if (totalAdmins <= 1)
                        return ResultadoOperacion<bool>.SetError(
                            "No puedes dar de baja la última cuenta de administrador.");
                }

                usuario.Estado = TipoEstadoUsuario.Suspendido;
                var tokens = await _context.TokensRefresco
                    .Where(t => t.UsuarioId == usuarioId && !t.Revocado)
                    .ToListAsync();
                foreach (var t in tokens)
                    t.Revocado = true;

                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en UsuarioPerfilServicio.SolicitarBajaAsync");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
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
