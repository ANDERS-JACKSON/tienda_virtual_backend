using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Comun.Enumeracion;

namespace TiendaVirtual.Dominio.Servicios.VendedorXqm
{
    /// <summary>Métricas públicas del vendedor calculadas desde datos reales (no columnas desactualizadas).</summary>
    public static class VendedorMetricasHelper
    {
        public static Task<int> ContarVentasEntregadasAsync(TiendaVirtualDbContext context, int vendedorId) =>
            context.Subordenes.CountAsync(s =>
                s.VendedorId == vendedorId &&
                s.Estado == TipoEstadoSuborden.Entregada);

        public static Task<int> ContarProductosActivosAsync(TiendaVirtualDbContext context, int vendedorId) =>
            context.Productos.CountAsync(p =>
                p.VendedorId == vendedorId &&
                p.Estado == TipoEstadoProducto.Activo);

        public static async Task<decimal> ObtenerCalificacionPromedioAsync(
            TiendaVirtualDbContext context,
            int vendedorId)
        {
            var promedio = await context.ResenasVendedor
                .Where(r => r.VendedorId == vendedorId)
                .Select(r => (double?)r.Calificacion)
                .AverageAsync();

            return promedio.HasValue ? Math.Round((decimal)promedio.Value, 2) : 0;
        }
    }
}
