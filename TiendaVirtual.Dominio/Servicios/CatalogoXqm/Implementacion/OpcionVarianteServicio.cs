using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;

namespace TiendaVirtual.Dominio.Servicios.CatalogoXqm.Implementacion
{
    public class OpcionVarianteServicio : IOpcionVarianteServicio
    {
        private readonly TiendaVirtualDbContext _context;
        private readonly ILogger<OpcionVarianteServicio> _logger;

        public OpcionVarianteServicio(
            TiendaVirtualDbContext context,
            ILogger<OpcionVarianteServicio> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ResultadoOperacion<List<OpcionVarianteDto>>> ListarActivasAsync()
        {
            try
            {
                var items = await _context.OpcionesVariante.AsNoTracking()
                    .Where(o => o.Activo)
                    .OrderBy(o => o.Orden)
                    .ThenBy(o => o.OpcionVarianteId)
                    .ToListAsync();
                return ResultadoOperacion<List<OpcionVarianteDto>>.SetExito(
                    items.Select(Mapear).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en OpcionVarianteServicio.ListarActivasAsync");
                return ResultadoOperacion<List<OpcionVarianteDto>>.SetError(
                    "Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        public async Task<ResultadoOperacion<List<OpcionVarianteDto>>> ListarAdminAsync()
        {
            try
            {
                var items = await _context.OpcionesVariante.AsNoTracking()
                    .OrderBy(o => o.Orden)
                    .ThenBy(o => o.OpcionVarianteId)
                    .ToListAsync();
                return ResultadoOperacion<List<OpcionVarianteDto>>.SetExito(
                    items.Select(Mapear).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en OpcionVarianteServicio.ListarAdminAsync");
                return ResultadoOperacion<List<OpcionVarianteDto>>.SetError(
                    "Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        public async Task<ResultadoOperacion<OpcionVarianteDto>> CrearAsync(CrearOpcionVarianteDto dto)
        {
            try
            {
                var nombre = dto?.Nombre?.Trim();
                if (string.IsNullOrWhiteSpace(nombre))
                    return ResultadoOperacion<OpcionVarianteDto>.SetError("El nombre es obligatorio.");

                if (await ExisteNombreAsync(nombre, null))
                    return ResultadoOperacion<OpcionVarianteDto>.SetError("Ya existe una opción con ese nombre.");

                var maxOrden = await _context.OpcionesVariante.MaxAsync(o => (int?)o.Orden) ?? 0;
                var entidad = new OpcionVariante
                {
                    Nombre = nombre,
                    Orden = dto!.Orden ?? maxOrden + 1,
                    Activo = true,
                };
                _context.OpcionesVariante.Add(entidad);
                await _context.SaveChangesAsync();
                return ResultadoOperacion<OpcionVarianteDto>.SetExito(Mapear(entidad));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en OpcionVarianteServicio.CrearAsync");
                return ResultadoOperacion<OpcionVarianteDto>.SetError(
                    "Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        public async Task<ResultadoOperacion<OpcionVarianteDto>> ActualizarAsync(
            int id, ActualizarOpcionVarianteDto dto)
        {
            try
            {
                var nombre = dto?.Nombre?.Trim();
                if (string.IsNullOrWhiteSpace(nombre))
                    return ResultadoOperacion<OpcionVarianteDto>.SetError("El nombre es obligatorio.");

                var entidad = await _context.OpcionesVariante.FindAsync(id);
                if (entidad == null)
                    return ResultadoOperacion<OpcionVarianteDto>.SetError("Opción no encontrada.");

                if (await ExisteNombreAsync(nombre, id))
                    return ResultadoOperacion<OpcionVarianteDto>.SetError("Ya existe una opción con ese nombre.");

                entidad.Nombre = nombre;
                entidad.Orden = dto!.Orden;
                entidad.Activo = dto.Activo;
                await _context.SaveChangesAsync();
                return ResultadoOperacion<OpcionVarianteDto>.SetExito(Mapear(entidad));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en OpcionVarianteServicio.ActualizarAsync");
                return ResultadoOperacion<OpcionVarianteDto>.SetError(
                    "Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        public async Task<ResultadoOperacion<bool>> ActivarAsync(int id)
        {
            return await CambiarActivoAsync(id, true);
        }

        public async Task<ResultadoOperacion<bool>> DesactivarAsync(int id)
        {
            return await CambiarActivoAsync(id, false);
        }

        public async Task<ResultadoOperacion<bool>> EliminarAsync(int id)
        {
            try
            {
                var entidad = await _context.OpcionesVariante.FindAsync(id);
                if (entidad == null)
                    return ResultadoOperacion<bool>.SetError("Opción no encontrada.");

                var enUso = await _context.VariantesProducto
                    .AnyAsync(v => v.OpcionVarianteId == id);
                if (enUso)
                {
                    entidad.Activo = false;
                    await _context.SaveChangesAsync();
                    return ResultadoOperacion<bool>.SetError(
                        "La opción está en uso. Se desactivó para que no aparezca en el combo.");
                }

                _context.OpcionesVariante.Remove(entidad);
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en OpcionVarianteServicio.EliminarAsync");
                return ResultadoOperacion<bool>.SetError(
                    "Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        private async Task<ResultadoOperacion<bool>> CambiarActivoAsync(int id, bool activo)
        {
            try
            {
                var entidad = await _context.OpcionesVariante.FindAsync(id);
                if (entidad == null)
                    return ResultadoOperacion<bool>.SetError("Opción no encontrada.");
                entidad.Activo = activo;
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en OpcionVarianteServicio.CambiarActivoAsync");
                return ResultadoOperacion<bool>.SetError(
                    "Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        private async Task<bool> ExisteNombreAsync(string nombre, int? exceptoId)
        {
            var q = _context.OpcionesVariante.Where(o => o.Nombre.ToLower() == nombre.ToLower());
            if (exceptoId != null)
                q = q.Where(o => o.OpcionVarianteId != exceptoId);
            return await q.AnyAsync();
        }

        private static OpcionVarianteDto Mapear(OpcionVariante o) => new()
        {
            OpcionVarianteId = o.OpcionVarianteId,
            Nombre = o.Nombre,
            Orden = o.Orden,
            Activo = o.Activo,
        };
    }
}
