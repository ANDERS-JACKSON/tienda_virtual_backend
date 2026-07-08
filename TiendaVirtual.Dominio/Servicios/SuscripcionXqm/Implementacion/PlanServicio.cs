using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Extensiones.VendedorXqm;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;

namespace TiendaVirtual.Dominio.Servicios.SuscripcionXqm.Implementacion
{
    public class PlanServicio : IPlanServicio
    {
        private readonly TiendaVirtualDbContext _context;
        private readonly ILogger<PlanServicio> _logger;

        public PlanServicio(TiendaVirtualDbContext context, ILogger<PlanServicio> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ResultadoOperacion<List<PlanDto>>> ListarActivosAsync()
        {
            try
            {
                var planes = await _context.Planes.AsNoTracking()
                    .Where(p => p.Activo)
                    .OrderBy(p => p.Precio)
                    .ToListAsync();
                return ResultadoOperacion<List<PlanDto>>.SetExito(planes.Select(p => p.ToDto()).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en PlanServicio.ListarActivosAsync");
                return ResultadoOperacion<List<PlanDto>>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<List<PlanDto>>> ListarTodosAsync()
        {
            try
            {
                var planes = await _context.Planes.AsNoTracking().OrderBy(p => p.Precio).ToListAsync();
                return ResultadoOperacion<List<PlanDto>>.SetExito(planes.Select(p => p.ToDto()).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en PlanServicio.ListarTodosAsync");
                return ResultadoOperacion<List<PlanDto>>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<PlanDto>> ObtenerPorIdAsync(int id)
        {
            try
            {
                var plan = await _context.Planes.AsNoTracking().FirstOrDefaultAsync(p => p.PlanId == id);
                if (plan == null)
                    return ResultadoOperacion<PlanDto>.SetError("Plan no encontrado.");
                return ResultadoOperacion<PlanDto>.SetExito(plan.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en PlanServicio.ObtenerPorIdAsync");
                return ResultadoOperacion<PlanDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        public async Task<ResultadoOperacion<PlanDto>> CrearAsync(CrearPlanDto dto)
        {
            try
            {
                var codigo = dto.Codigo.Trim().ToUpperInvariant();
                if (await _context.Planes.AnyAsync(p => p.Codigo == codigo))
                    return ResultadoOperacion<PlanDto>.SetError("Ya existe un plan con ese código.");

                var plan = new Plan
                {
                    Codigo = codigo,
                    Nombre = dto.Nombre.Trim(),
                    Descripcion = dto.Descripcion?.Trim(),
                    Precio = dto.Precio,
                    Periodo = (TipoPeriodoPlan)dto.Periodo.Id,
                    MaxProductos = dto.MaxProductos,
                    TasaComision = dto.TasaComision,
                    Activo = true
                };
                _context.Planes.Add(plan);
                await _context.SaveChangesAsync();
                return ResultadoOperacion<PlanDto>.SetExito(plan.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en PlanServicio.CrearAsync");
                return ResultadoOperacion<PlanDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<PlanDto>> ActualizarAsync(int id, ActualizarPlanDto dto)
        {
            try
            {
                var plan = await _context.Planes.FirstOrDefaultAsync(p => p.PlanId == id);
                if (plan == null)
                    return ResultadoOperacion<PlanDto>.SetError("Plan no encontrado.");

                var nuevoCodigo = dto.Codigo.Trim().ToUpperInvariant();
                if (nuevoCodigo != plan.Codigo &&
                    await _context.Planes.AnyAsync(p => p.Codigo == nuevoCodigo && p.PlanId != id))
                    return ResultadoOperacion<PlanDto>.SetError("Ya existe otro plan con ese código.");

                plan.Codigo = nuevoCodigo;
                plan.Nombre = dto.Nombre.Trim();
                plan.Descripcion = dto.Descripcion?.Trim();
                plan.Precio = dto.Precio;
                plan.Periodo = (TipoPeriodoPlan)dto.Periodo.Id;
                plan.MaxProductos = dto.MaxProductos;
                plan.TasaComision = dto.TasaComision;
                plan.Activo = dto.Activo;

                await _context.SaveChangesAsync();
                return ResultadoOperacion<PlanDto>.SetExito(plan.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en PlanServicio.ActualizarAsync");
                return ResultadoOperacion<PlanDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<bool>> CambiarEstadoAsync(int id, bool activo)
        {
            try
            {
                var plan = await _context.Planes.FirstOrDefaultAsync(p => p.PlanId == id);
                if (plan == null)
                    return ResultadoOperacion<bool>.SetError("Plan no encontrado.");

                if (!activo)
                {
                    var enUso = await _context.Suscripciones.AnyAsync(s =>
                        s.PlanId == id &&
                        (s.Estado == TipoEstadoSuscripcion.EnPrueba ||
                         s.Estado == TipoEstadoSuscripcion.Activa));
                    if (enUso)
                        return ResultadoOperacion<bool>.SetError(
                            "No puedes desactivar el plan: hay vendedores con suscripciones activas en él.");
                }

                plan.Activo = activo;
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en PlanServicio.CambiarEstadoAsync");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }
        }
    }
}
