using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;

namespace TiendaVirtual.Dominio.Servicios.SeguridadXqm.Implementacion
{
    public class UbigeoAdminServicio : IUbigeoAdminServicio
    {
        private static readonly Regex RxDep = new(@"^\d{2}$", RegexOptions.Compiled);
        private static readonly Regex RxProv = new(@"^\d{4}$", RegexOptions.Compiled);
        private static readonly Regex RxDist = new(@"^\d{6}$", RegexOptions.Compiled);

        private readonly TiendaVirtualDbContext _context;
        private readonly ILogger<UbigeoAdminServicio> _logger;

        public UbigeoAdminServicio(TiendaVirtualDbContext context, ILogger<UbigeoAdminServicio> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ── Departamentos ──────────────────────────────────────

        public async Task<ResultadoOperacion<List<DepartamentoAdminDto>>> ListarDepartamentosAsync()
        {
            try
            {
                var list = await _context.Departamentos.AsNoTracking()
                    .OrderBy(d => d.DepartamentoId)
                    .Select(d => new DepartamentoAdminDto
                    {
                        DepartamentoId = d.DepartamentoId,
                        Nombre = d.Nombre,
                        TotalProvincias = d.Provincias.Count
                    })
                    .ToListAsync();
                return ResultadoOperacion<List<DepartamentoAdminDto>>.SetExito(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ListarDepartamentosAsync");
                return ResultadoOperacion<List<DepartamentoAdminDto>>.SetError("Ocurrió un error inesperado.");
            }
        }

        public async Task<ResultadoOperacion<DepartamentoAdminDto>> CrearDepartamentoAsync(CrearDepartamentoDto dto)
        {
            try
            {
                var id = NormalizarId(dto.DepartamentoId);
                var nombre = NormalizarNombre(dto.Nombre);
                if (!RxDep.IsMatch(id))
                    return ResultadoOperacion<DepartamentoAdminDto>.SetError("El ID de departamento debe ser 2 dígitos (ej. 15).");
                if (nombre == null)
                    return ResultadoOperacion<DepartamentoAdminDto>.SetError("El nombre es obligatorio.");
                if (await _context.Departamentos.AnyAsync(d => d.DepartamentoId == id))
                    return ResultadoOperacion<DepartamentoAdminDto>.SetError("Ya existe ese departamento.");

                _context.Departamentos.Add(new Departamento { DepartamentoId = id, Nombre = nombre });
                await _context.SaveChangesAsync();
                return ResultadoOperacion<DepartamentoAdminDto>.SetExito(new DepartamentoAdminDto
                {
                    DepartamentoId = id,
                    Nombre = nombre,
                    TotalProvincias = 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error CrearDepartamentoAsync");
                return ResultadoOperacion<DepartamentoAdminDto>.SetError("Ocurrió un error inesperado.");
            }
        }

        public async Task<ResultadoOperacion<DepartamentoAdminDto>> ActualizarDepartamentoAsync(
            string id, ActualizarDepartamentoDto dto)
        {
            try
            {
                id = NormalizarId(id);
                var nombre = NormalizarNombre(dto.Nombre);
                if (nombre == null)
                    return ResultadoOperacion<DepartamentoAdminDto>.SetError("El nombre es obligatorio.");

                var dep = await _context.Departamentos.FirstOrDefaultAsync(d => d.DepartamentoId == id);
                if (dep == null)
                    return ResultadoOperacion<DepartamentoAdminDto>.SetError("Departamento no encontrado.");

                dep.Nombre = nombre;
                await _context.SaveChangesAsync();

                var total = await _context.Provincias.CountAsync(p => p.DepartamentoId == id);
                return ResultadoOperacion<DepartamentoAdminDto>.SetExito(new DepartamentoAdminDto
                {
                    DepartamentoId = dep.DepartamentoId,
                    Nombre = dep.Nombre,
                    TotalProvincias = total
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ActualizarDepartamentoAsync");
                return ResultadoOperacion<DepartamentoAdminDto>.SetError("Ocurrió un error inesperado.");
            }
        }

        public async Task<ResultadoOperacion<bool>> EliminarDepartamentoAsync(string id)
        {
            try
            {
                id = NormalizarId(id);
                var dep = await _context.Departamentos.FirstOrDefaultAsync(d => d.DepartamentoId == id);
                if (dep == null)
                    return ResultadoOperacion<bool>.SetError("Departamento no encontrado.");

                if (await _context.Provincias.AnyAsync(p => p.DepartamentoId == id))
                    return ResultadoOperacion<bool>.SetError(
                        "No se puede eliminar: tiene provincias asociadas. Elimina primero las provincias.");

                _context.Departamentos.Remove(dep);
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error EliminarDepartamentoAsync");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado.");
            }
        }

        // ── Provincias ─────────────────────────────────────────

        public async Task<ResultadoOperacion<List<ProvinciaAdminDto>>> ListarProvinciasAsync(string? departamentoId)
        {
            try
            {
                var q = _context.Provincias.AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(departamentoId))
                {
                    var dep = NormalizarId(departamentoId);
                    q = q.Where(p => p.DepartamentoId == dep);
                }

                var list = await q
                    .OrderBy(p => p.ProvinciaId)
                    .Select(p => new ProvinciaAdminDto
                    {
                        ProvinciaId = p.ProvinciaId,
                        DepartamentoId = p.DepartamentoId,
                        NombreDepartamento = p.Departamento.Nombre,
                        Nombre = p.Nombre,
                        TotalDistritos = p.Distritos.Count
                    })
                    .ToListAsync();

                return ResultadoOperacion<List<ProvinciaAdminDto>>.SetExito(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ListarProvinciasAsync");
                return ResultadoOperacion<List<ProvinciaAdminDto>>.SetError("Ocurrió un error inesperado.");
            }
        }

        public async Task<ResultadoOperacion<ProvinciaAdminDto>> CrearProvinciaAsync(CrearProvinciaDto dto)
        {
            try
            {
                var id = NormalizarId(dto.ProvinciaId);
                var depId = NormalizarId(dto.DepartamentoId);
                var nombre = NormalizarNombre(dto.Nombre);

                if (!RxProv.IsMatch(id))
                    return ResultadoOperacion<ProvinciaAdminDto>.SetError("El ID de provincia debe ser 4 dígitos (ej. 1501).");
                if (!RxDep.IsMatch(depId))
                    return ResultadoOperacion<ProvinciaAdminDto>.SetError("El departamento debe ser 2 dígitos.");
                if (!id.StartsWith(depId, StringComparison.Ordinal))
                    return ResultadoOperacion<ProvinciaAdminDto>.SetError(
                        "El ID de provincia debe comenzar con el ID del departamento.");
                if (nombre == null)
                    return ResultadoOperacion<ProvinciaAdminDto>.SetError("El nombre es obligatorio.");

                var dep = await _context.Departamentos.AsNoTracking()
                    .FirstOrDefaultAsync(d => d.DepartamentoId == depId);
                if (dep == null)
                    return ResultadoOperacion<ProvinciaAdminDto>.SetError("El departamento no existe.");
                if (await _context.Provincias.AnyAsync(p => p.ProvinciaId == id))
                    return ResultadoOperacion<ProvinciaAdminDto>.SetError("Ya existe esa provincia.");

                _context.Provincias.Add(new Provincia
                {
                    ProvinciaId = id,
                    DepartamentoId = depId,
                    Nombre = nombre
                });
                await _context.SaveChangesAsync();

                return ResultadoOperacion<ProvinciaAdminDto>.SetExito(new ProvinciaAdminDto
                {
                    ProvinciaId = id,
                    DepartamentoId = depId,
                    NombreDepartamento = dep.Nombre,
                    Nombre = nombre,
                    TotalDistritos = 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error CrearProvinciaAsync");
                return ResultadoOperacion<ProvinciaAdminDto>.SetError("Ocurrió un error inesperado.");
            }
        }

        public async Task<ResultadoOperacion<ProvinciaAdminDto>> ActualizarProvinciaAsync(
            string id, ActualizarProvinciaDto dto)
        {
            try
            {
                id = NormalizarId(id);
                var nombre = NormalizarNombre(dto.Nombre);
                if (nombre == null)
                    return ResultadoOperacion<ProvinciaAdminDto>.SetError("El nombre es obligatorio.");

                var prov = await _context.Provincias
                    .Include(p => p.Departamento)
                    .FirstOrDefaultAsync(p => p.ProvinciaId == id);
                if (prov == null)
                    return ResultadoOperacion<ProvinciaAdminDto>.SetError("Provincia no encontrada.");

                prov.Nombre = nombre;
                await _context.SaveChangesAsync();

                var total = await _context.Distritos.CountAsync(d => d.ProvinciaId == id);
                return ResultadoOperacion<ProvinciaAdminDto>.SetExito(new ProvinciaAdminDto
                {
                    ProvinciaId = prov.ProvinciaId,
                    DepartamentoId = prov.DepartamentoId,
                    NombreDepartamento = prov.Departamento.Nombre,
                    Nombre = prov.Nombre,
                    TotalDistritos = total
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ActualizarProvinciaAsync");
                return ResultadoOperacion<ProvinciaAdminDto>.SetError("Ocurrió un error inesperado.");
            }
        }

        public async Task<ResultadoOperacion<bool>> EliminarProvinciaAsync(string id)
        {
            try
            {
                id = NormalizarId(id);
                var prov = await _context.Provincias.FirstOrDefaultAsync(p => p.ProvinciaId == id);
                if (prov == null)
                    return ResultadoOperacion<bool>.SetError("Provincia no encontrada.");

                if (await _context.Distritos.AnyAsync(d => d.ProvinciaId == id))
                    return ResultadoOperacion<bool>.SetError(
                        "No se puede eliminar: tiene distritos asociados. Elimina primero los distritos.");

                _context.Provincias.Remove(prov);
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error EliminarProvinciaAsync");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado.");
            }
        }

        // ── Distritos ──────────────────────────────────────────

        public async Task<ResultadoOperacion<List<DistritoAdminDto>>> ListarDistritosAsync(string? provinciaId)
        {
            try
            {
                var q = _context.Distritos.AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(provinciaId))
                {
                    var pid = NormalizarId(provinciaId);
                    q = q.Where(d => d.ProvinciaId == pid);
                }

                var list = await q
                    .OrderBy(d => d.DistritoId)
                    .Select(d => new DistritoAdminDto
                    {
                        DistritoId = d.DistritoId,
                        ProvinciaId = d.ProvinciaId,
                        NombreProvincia = d.Provincia.Nombre,
                        DepartamentoId = d.Provincia.DepartamentoId,
                        NombreDepartamento = d.Provincia.Departamento.Nombre,
                        Nombre = d.Nombre
                    })
                    .ToListAsync();

                return ResultadoOperacion<List<DistritoAdminDto>>.SetExito(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ListarDistritosAsync");
                return ResultadoOperacion<List<DistritoAdminDto>>.SetError("Ocurrió un error inesperado.");
            }
        }

        public async Task<ResultadoOperacion<DistritoAdminDto>> CrearDistritoAsync(CrearDistritoDto dto)
        {
            try
            {
                var id = NormalizarId(dto.DistritoId);
                var provId = NormalizarId(dto.ProvinciaId);
                var nombre = NormalizarNombre(dto.Nombre);

                if (!RxDist.IsMatch(id))
                    return ResultadoOperacion<DistritoAdminDto>.SetError("El ID de distrito debe ser 6 dígitos (ej. 150101).");
                if (!RxProv.IsMatch(provId))
                    return ResultadoOperacion<DistritoAdminDto>.SetError("La provincia debe ser 4 dígitos.");
                if (!id.StartsWith(provId, StringComparison.Ordinal))
                    return ResultadoOperacion<DistritoAdminDto>.SetError(
                        "El ID de distrito debe comenzar con el ID de la provincia.");
                if (nombre == null)
                    return ResultadoOperacion<DistritoAdminDto>.SetError("El nombre es obligatorio.");

                var prov = await _context.Provincias.AsNoTracking()
                    .Include(p => p.Departamento)
                    .FirstOrDefaultAsync(p => p.ProvinciaId == provId);
                if (prov == null)
                    return ResultadoOperacion<DistritoAdminDto>.SetError("La provincia no existe.");
                if (await _context.Distritos.AnyAsync(d => d.DistritoId == id))
                    return ResultadoOperacion<DistritoAdminDto>.SetError("Ya existe ese distrito.");

                // Evitar borrar si hay direcciones que lo usan
                _context.Distritos.Add(new Distrito
                {
                    DistritoId = id,
                    ProvinciaId = provId,
                    Nombre = nombre
                });
                await _context.SaveChangesAsync();

                return ResultadoOperacion<DistritoAdminDto>.SetExito(new DistritoAdminDto
                {
                    DistritoId = id,
                    ProvinciaId = provId,
                    NombreProvincia = prov.Nombre,
                    DepartamentoId = prov.DepartamentoId,
                    NombreDepartamento = prov.Departamento.Nombre,
                    Nombre = nombre
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error CrearDistritoAsync");
                return ResultadoOperacion<DistritoAdminDto>.SetError("Ocurrió un error inesperado.");
            }
        }

        public async Task<ResultadoOperacion<DistritoAdminDto>> ActualizarDistritoAsync(
            string id, ActualizarDistritoDto dto)
        {
            try
            {
                id = NormalizarId(id);
                var nombre = NormalizarNombre(dto.Nombre);
                if (nombre == null)
                    return ResultadoOperacion<DistritoAdminDto>.SetError("El nombre es obligatorio.");

                var dist = await _context.Distritos
                    .Include(d => d.Provincia).ThenInclude(p => p.Departamento)
                    .FirstOrDefaultAsync(d => d.DistritoId == id);
                if (dist == null)
                    return ResultadoOperacion<DistritoAdminDto>.SetError("Distrito no encontrado.");

                dist.Nombre = nombre;
                await _context.SaveChangesAsync();

                return ResultadoOperacion<DistritoAdminDto>.SetExito(new DistritoAdminDto
                {
                    DistritoId = dist.DistritoId,
                    ProvinciaId = dist.ProvinciaId,
                    NombreProvincia = dist.Provincia.Nombre,
                    DepartamentoId = dist.Provincia.DepartamentoId,
                    NombreDepartamento = dist.Provincia.Departamento.Nombre,
                    Nombre = dist.Nombre
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ActualizarDistritoAsync");
                return ResultadoOperacion<DistritoAdminDto>.SetError("Ocurrió un error inesperado.");
            }
        }

        public async Task<ResultadoOperacion<bool>> EliminarDistritoAsync(string id)
        {
            try
            {
                id = NormalizarId(id);
                var dist = await _context.Distritos.FirstOrDefaultAsync(d => d.DistritoId == id);
                if (dist == null)
                    return ResultadoOperacion<bool>.SetError("Distrito no encontrado.");

                var enUso = await _context.Direcciones.AnyAsync(d => d.DistritoId == id);
                if (enUso)
                    return ResultadoOperacion<bool>.SetError(
                        "No se puede eliminar: hay direcciones de usuarios que usan este distrito.");

                _context.Distritos.Remove(dist);
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error EliminarDistritoAsync");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado.");
            }
        }

        private static string NormalizarId(string? id) => (id ?? string.Empty).Trim();

        private static string? NormalizarNombre(string? nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return null;
            return nombre.Trim();
        }
    }
}
