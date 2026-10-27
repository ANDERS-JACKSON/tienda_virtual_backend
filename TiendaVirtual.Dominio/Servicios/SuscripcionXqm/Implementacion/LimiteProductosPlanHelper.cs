using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;

namespace TiendaVirtual.Dominio.Servicios.SuscripcionXqm.Implementacion
{
    /// <summary>
    /// Cupo de productos del plan. Cuentan los productos en la tienda (activos y borradores);
    /// los pausados no ocupan cupo.
    /// </summary>
    internal static class LimiteProductosPlanHelper
    {
        public static Task<int> ContarEnUsoAsync(
            TiendaVirtualDbContext context, int vendedorId, int? excluirProductoId = null) =>
            context.Productos.CountAsync(p =>
                p.VendedorId == vendedorId &&
                (p.Estado == TipoEstadoProducto.Activo || p.Estado == TipoEstadoProducto.Borrador) &&
                (!excluirProductoId.HasValue || p.ProductoId != excluirProductoId.Value));

        /// <summary>Plan de la suscripción comercialmente vigente, o null si no hay.</summary>
        public static async Task<Plan?> ObtenerPlanVigenteAsync(
            TiendaVirtualDbContext context, int vendedorId, DateTime now)
        {
            var suscripciones = await context.Suscripciones.AsNoTracking()
                .Include(s => s.Plan)
                .Where(s => s.VendedorId == vendedorId &&
                            (s.Estado == TipoEstadoSuscripcion.EnPrueba ||
                             s.Estado == TipoEstadoSuscripcion.Activa))
                .OrderByDescending(s => s.SuscripcionId)
                .ToListAsync();

            return suscripciones
                .FirstOrDefault(s => SuscripcionBeneficiosHelper.EsComercialmenteActiva(s, now))
                ?.Plan;
        }

        public static string MensajeLimiteAlcanzado(Plan plan) =>
            $"Alcanzaste el máximo de {plan.MaxProductos} productos de tu plan {plan.Nombre}. " +
            "Sube a un plan superior para publicar más, o pausa un producto para liberar espacio.";
    }
}
