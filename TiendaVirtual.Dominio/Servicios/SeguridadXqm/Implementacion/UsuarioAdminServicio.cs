using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;
using TiendaVirtual.Dominio.Servicios.SoporteXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Dominio.Servicios.SeguridadXqm.Implementacion
{
    public class UsuarioAdminServicio : IUsuarioAdminServicio
    {
        private const int LONGITUD_CLAVE = 10;

        private readonly TiendaVirtualDbContext _context;
        private readonly INotificacionServicio _notificacionServicio;

        public UsuarioAdminServicio(TiendaVirtualDbContext context, INotificacionServicio notificacionServicio)
        {
            _context = context;
            _notificacionServicio = notificacionServicio;
        }

        public async Task<ResultadoOperacion<PaginacionRespuestaDto<UsuarioAdminListadoDto>>> ListarAsync(
            string? busqueda, int? rolId, string? estado, int pagina, int tamanioPagina)
        {
            try
            {
                pagina = Math.Max(1, pagina);
                tamanioPagina = Math.Clamp(tamanioPagina, 1, 50);

                var query = _context.Usuarios.AsNoTracking()
                    .Include(u => u.Persona)
                    .Include(u => u.UsuarioRoles).ThenInclude(ur => ur.Rol)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    var term = busqueda.Trim().ToLower();
                    query = query.Where(u =>
                        u.Correo.ToLower().Contains(term) ||
                        (u.Persona.Nombres + " " + u.Persona.ApellidoPaterno).ToLower().Contains(term) ||
                        u.Persona.NumeroDocumento.Contains(term));
                }

                if (rolId.HasValue)
                    query = query.Where(u => u.UsuarioRoles.Any(ur => ur.RolId == rolId.Value));

                if (!string.IsNullOrWhiteSpace(estado) && estado != "todos")
                {
                    var activo = estado.Equals("activo", StringComparison.OrdinalIgnoreCase);
                    query = activo
                        ? query.Where(u => u.Estado == TipoEstadoUsuario.Activo)
                        : query.Where(u => u.Estado != TipoEstadoUsuario.Activo);
                }

                var total = await query.CountAsync();
                var usuarios = await query
                    .OrderByDescending(u => u.FechaAlta)
                    .Skip((pagina - 1) * tamanioPagina)
                    .Take(tamanioPagina)
                    .ToListAsync();

                var items = usuarios.Select(MapearListado).ToList();

                return ResultadoOperacion<PaginacionRespuestaDto<UsuarioAdminListadoDto>>.SetExito(
                    new PaginacionRespuestaDto<UsuarioAdminListadoDto>
                    {
                        Items = items,
                        Pagina = pagina,
                        TamanioPagina = tamanioPagina,
                        TotalRegistros = total,
                        HayMas = pagina * tamanioPagina < total
                    });
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<PaginacionRespuestaDto<UsuarioAdminListadoDto>>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<UsuarioAdminDetalleDto>> ObtenerDetalleAsync(int usuarioId)
        {
            try
            {
                var u = await _context.Usuarios.AsNoTracking()
                    .Include(x => x.Persona)
                    .Include(x => x.UsuarioRoles).ThenInclude(ur => ur.Rol)
                    .Include(x => x.Vendedor)
                    .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId);

                if (u == null)
                    return ResultadoOperacion<UsuarioAdminDetalleDto>.SetError("Usuario no encontrado.");

                var totalOrdenes = await _context.Ordenes.CountAsync(o => o.ClienteId == usuarioId);
                var totalGastado = await _context.Ordenes
                    .Where(o => o.ClienteId == usuarioId && o.Estado != TipoEstadoOrden.Cancelada)
                    .SumAsync(o => (decimal?)o.Total) ?? 0;

                var dto = MapearListado(u);
                var detalle = new UsuarioAdminDetalleDto
                {
                    UsuarioId = dto.UsuarioId,
                    Correo = dto.Correo,
                    NombreCompleto = dto.NombreCompleto,
                    DocumentoIdentidad = dto.DocumentoIdentidad,
                    Roles = dto.Roles,
                    Activo = dto.Activo,
                    FechaCreacion = dto.FechaCreacion,
                    UltimoAcceso = dto.UltimoAcceso,
                    Telefono = u.Persona.Telefono,
                    FechaNacimiento = u.Persona.FechaNacimiento?.ToDateTime(TimeOnly.MinValue),
                    VendedorId = u.Vendedor?.VendedorId,
                    NombreTienda = u.Vendedor?.NombreTienda,
                    TotalOrdenes = totalOrdenes,
                    TotalGastado = totalGastado,
                    Tiene2FA = u.TwoFactorEnabled && !string.IsNullOrEmpty(u.TwoFactorSecret)
                };

                return ResultadoOperacion<UsuarioAdminDetalleDto>.SetExito(detalle);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<UsuarioAdminDetalleDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> ActivarAsync(int usuarioId)
        {
            try
            {
                var u = await _context.Usuarios.FindAsync(usuarioId);
                if (u == null) return ResultadoOperacion<bool>.SetError("Usuario no encontrado.");
                u.Estado = TipoEstadoUsuario.Activo;
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> DesactivarAsync(int usuarioId)
        {
            try
            {
                var u = await _context.Usuarios
                    .Include(x => x.UsuarioRoles).ThenInclude(ur => ur.Rol)
                    .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId);
                if (u == null) return ResultadoOperacion<bool>.SetError("Usuario no encontrado.");

                if (u.UsuarioRoles.Any(ur => ur.Rol.Nombre == "ADMIN"))
                {
                    var totalAdmins = await _context.UsuarioRoles
                        .CountAsync(ur => ur.Rol.Nombre == "ADMIN" &&
                            _context.Usuarios.Any(uu => uu.UsuarioId == ur.UsuarioId &&
                                uu.Estado == TipoEstadoUsuario.Activo));
                    if (totalAdmins <= 1)
                        return ResultadoOperacion<bool>.SetError("No se puede desactivar al último administrador activo.");
                }

                u.Estado = TipoEstadoUsuario.Suspendido;
                var tokens = await _context.TokensRefresco
                    .Where(t => t.UsuarioId == usuarioId && !t.Revocado)
                    .ToListAsync();
                foreach (var t in tokens) t.Revocado = true;

                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> AsignarRolAsync(int usuarioId, int rolId)
        {
            try
            {
                if (!await _context.Usuarios.AnyAsync(u => u.UsuarioId == usuarioId))
                    return ResultadoOperacion<bool>.SetError("Usuario no encontrado.");
                if (!await _context.Roles.AnyAsync(r => r.RolId == rolId))
                    return ResultadoOperacion<bool>.SetError("Rol no encontrado.");

                var existe = await _context.UsuarioRoles
                    .AnyAsync(ur => ur.UsuarioId == usuarioId && ur.RolId == (short)rolId);
                if (existe)
                    return ResultadoOperacion<bool>.SetExito(true);

                _context.UsuarioRoles.Add(new UsuarioRol
                {
                    UsuarioId = usuarioId,
                    RolId = (short)rolId
                });
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> QuitarRolAsync(int usuarioId, int rolId, int adminActualId)
        {
            try
            {
                if (usuarioId == adminActualId && rolId == (int)TipoRol.Admin)
                    return ResultadoOperacion<bool>.SetError("No puedes quitarte el rol ADMIN a ti mismo.");

                var ur = await _context.UsuarioRoles
                    .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId && x.RolId == (short)rolId);
                if (ur == null)
                    return ResultadoOperacion<bool>.SetExito(true);

                if (rolId == (int)TipoRol.Admin)
                {
                    var totalAdmins = await _context.UsuarioRoles
                        .CountAsync(x => x.RolId == (short)TipoRol.Admin);
                    if (totalAdmins <= 1)
                        return ResultadoOperacion<bool>.SetError("Debe existir al menos un administrador.");
                }

                _context.UsuarioRoles.Remove(ur);
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> ResetearClaveAsync(int usuarioId)
        {
            try
            {
                var u = await _context.Usuarios.FirstOrDefaultAsync(x => x.UsuarioId == usuarioId);
                if (u == null) return ResultadoOperacion<bool>.SetError("Usuario no encontrado.");

                var clave = GenerarClaveAleatoria(LONGITUD_CLAVE);
                u.Contrasena = BCrypt.Net.BCrypt.HashPassword(clave);
                u.ForzarCambioClave = true;
                await _context.SaveChangesAsync();

                await _notificacionServicio.CrearAsync(
                    usuarioId,
                    TipoNotificacion.CreacionUsuario,
                    "Contraseña restablecida",
                    "Un administrador restableció tu contraseña. Revisa tu correo.",
                    null,
                    plantillaEmail: PlantillaCorreo.CreacionUsuario,
                    placeholdersEmail: new Dictionary<string, string>
                    {
                        ["usuario"] = u.Correo,
                        ["clave"] = clave
                    });

                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<List<RolDto>>> ListarRolesAsync()
        {
            try
            {
                var roles = await _context.Roles.AsNoTracking()
                    .OrderBy(r => r.RolId)
                    .Select(r => new RolDto { RolId = r.RolId, Nombre = r.Nombre })
                    .ToListAsync();
                return ResultadoOperacion<List<RolDto>>.SetExito(roles);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<List<RolDto>>.SetError("Error: " + ex.Message);
            }
        }

        private static UsuarioAdminListadoDto MapearListado(Usuario u) => new()
        {
            UsuarioId = u.UsuarioId,
            Correo = u.Correo,
            NombreCompleto = $"{u.Persona.Nombres} {u.Persona.ApellidoPaterno}".Trim(),
            DocumentoIdentidad = u.Persona.NumeroDocumento,
            Roles = u.UsuarioRoles.Select(ur => ur.Rol.Nombre).ToList(),
            Activo = u.Estado == TipoEstadoUsuario.Activo,
            FechaCreacion = u.FechaAlta,
            UltimoAcceso = u.UltimoAcceso
        };

        private static string GenerarClaveAleatoria(int longitud)
        {
            const string TODO = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var buf = new byte[longitud];
            rng.GetBytes(buf);
            var chars = new char[longitud];
            for (int i = 0; i < longitud; i++)
                chars[i] = TODO[buf[i] % TODO.Length];
            return new string(chars);
        }
    }
}
