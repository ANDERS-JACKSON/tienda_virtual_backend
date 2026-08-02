using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;

namespace TiendaVirtual.Dominio.Servicios.SeguridadXqm.Implementacion
{
    public class UbigeoServicio : IUbigeoServicio
    {
        private readonly TiendaVirtualDbContext _context;
        private readonly ILogger<UbigeoServicio> _logger;

        public UbigeoServicio(TiendaVirtualDbContext context, ILogger<UbigeoServicio> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ResultadoOperacion<List<UbigeoItemDto>>> ListarDepartamentosAsync()
        {
            try
            {
                var items = await _context.Departamentos.AsNoTracking()
                    .OrderBy(d => d.Nombre)
                    .Select(d => new UbigeoItemDto
                    {
                        Id = d.DepartamentoId,
                        Nombre = d.Nombre,
                    })
                    .ToListAsync();

                return ResultadoOperacion<List<UbigeoItemDto>>.SetExito(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en UbigeoServicio.ListarDepartamentosAsync");
                return ResultadoOperacion<List<UbigeoItemDto>>.SetError(
                    "No se pudo cargar el catálogo de departamentos.");
            }
        }

        public async Task<ResultadoOperacion<List<UbigeoItemDto>>> ListarProvinciasAsync(string departamentoId)
        {
            try
            {
                var id = (departamentoId ?? string.Empty).Trim();
                if (id.Length != 2)
                    return ResultadoOperacion<List<UbigeoItemDto>>.SetError("Departamento inválido.");

                var items = await _context.Provincias.AsNoTracking()
                    .Where(p => p.DepartamentoId == id)
                    .OrderBy(p => p.Nombre)
                    .Select(p => new UbigeoItemDto
                    {
                        Id = p.ProvinciaId,
                        Nombre = p.Nombre,
                    })
                    .ToListAsync();

                return ResultadoOperacion<List<UbigeoItemDto>>.SetExito(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en UbigeoServicio.ListarProvinciasAsync");
                return ResultadoOperacion<List<UbigeoItemDto>>.SetError(
                    "No se pudo cargar el catálogo de provincias.");
            }
        }

        public async Task<ResultadoOperacion<List<UbigeoItemDto>>> ListarDistritosAsync(string provinciaId)
        {
            try
            {
                var id = (provinciaId ?? string.Empty).Trim();
                if (id.Length != 4)
                    return ResultadoOperacion<List<UbigeoItemDto>>.SetError("Provincia inválida.");

                var items = await _context.Distritos.AsNoTracking()
                    .Where(d => d.ProvinciaId == id)
                    .OrderBy(d => d.Nombre)
                    .Select(d => new UbigeoItemDto
                    {
                        Id = d.DistritoId,
                        Nombre = d.Nombre,
                    })
                    .ToListAsync();

                return ResultadoOperacion<List<UbigeoItemDto>>.SetExito(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en UbigeoServicio.ListarDistritosAsync");
                return ResultadoOperacion<List<UbigeoItemDto>>.SetError(
                    "No se pudo cargar el catálogo de distritos.");
            }
        }
    }
}
