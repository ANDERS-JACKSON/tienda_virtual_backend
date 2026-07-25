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
                var nombres = await MapaNombresAsync();
                return ResultadoOperacion<List<PlanDto>>.SetExito(
                    planes.Select(p => p.ToDto(nombres)).ToList());
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
                var nombres = await MapaNombresAsync();
                return ResultadoOperacion<List<PlanDto>>.SetExito(
                    planes.Select(p => p.ToDto(nombres)).ToList());
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
                var nombres = await MapaNombresAsync();
                return ResultadoOperacion<PlanDto>.SetExito(plan.ToDto(nombres));
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

                var errorBeneficios = await ValidarBeneficiosAsync(dto.Beneficios, planIdExcluir: null);
                if (errorBeneficios != null)
                    return ResultadoOperacion<PlanDto>.SetError(errorBeneficios);

                var plan = new Plan
                {
                    Codigo = codigo,
                    Nombre = dto.Nombre.Trim(),
                    Descripcion = dto.Descripcion?.Trim(),
                    Precio = dto.Precio,
                    Periodo = (TipoPeriodoPlan)dto.Periodo.Id,
                    MaxProductos = dto.MaxProductos,
                    TasaComision = dto.TasaComision,
                    Activo = true,
                    Beneficios = PlanExtension.SerializarBeneficios(
                        dto.Beneficios ?? BeneficiosPorDefecto())
                };
                _context.Planes.Add(plan);
                await _context.SaveChangesAsync();

                var nombres = await MapaNombresAsync();
                return ResultadoOperacion<PlanDto>.SetExito(plan.ToDto(nombres));
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

                var errorBeneficios = await ValidarBeneficiosAsync(dto.Beneficios, planIdExcluir: id);
                if (errorBeneficios != null)
                    return ResultadoOperacion<PlanDto>.SetError(errorBeneficios);

                // El código del plan es inmutable: no se actualiza nunca.
                plan.Nombre = dto.Nombre.Trim();
                plan.Descripcion = dto.Descripcion?.Trim();
                plan.Precio = dto.Precio;
                plan.Periodo = (TipoPeriodoPlan)dto.Periodo.Id;
                plan.MaxProductos = dto.MaxProductos;
                plan.TasaComision = dto.TasaComision;
                plan.Activo = dto.Activo;
                if (dto.Beneficios != null)
                    plan.Beneficios = PlanExtension.SerializarBeneficios(dto.Beneficios);

                await _context.SaveChangesAsync();

                var nombres = await MapaNombresAsync();
                return ResultadoOperacion<PlanDto>.SetExito(plan.ToDto(nombres));
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

        private async Task<Dictionary<int, string>> MapaNombresAsync()
        {
            return await _context.Planes.AsNoTracking()
                .Select(p => new { p.PlanId, p.Nombre })
                .ToDictionaryAsync(x => x.PlanId, x => x.Nombre);
        }

        private async Task<string?> ValidarBeneficiosAsync(PlanBeneficiosDto? beneficios, int? planIdExcluir)
        {
            if (beneficios?.HeredaDePlanId is not int baseId)
                return null;

            if (planIdExcluir.HasValue && baseId == planIdExcluir.Value)
                return "Un plan no puede heredar de sí mismo.";

            var existe = await _context.Planes.AsNoTracking()
                .AnyAsync(p => p.PlanId == baseId);
            if (!existe)
                return "El plan del que hereda no existe.";

            return null;
        }

        private static PlanBeneficiosDto BeneficiosPorDefecto() => new()
        {
            Etiqueta = "Incluye",
            Destacado = false,
            Items = new List<PlanBeneficioItemDto>
            {
                new() { Texto = "Tienda pública en el marketplace", Incluido = true },
                new() { Texto = "Panel de pedidos y productos", Incluido = true }
            }
        };
    }
}
