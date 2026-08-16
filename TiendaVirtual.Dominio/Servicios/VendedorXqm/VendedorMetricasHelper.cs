using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.VentaXqm;

namespace TiendaVirtual.Dominio.Servicios.VendedorXqm
{
    /// <summary>Métricas públicas del vendedor calculadas desde datos reales (no columnas desactualizadas).</summary>
    public static class VendedorMetricasHelper
    {
        /// <summary>
        /// Ítems de pedidos pagados del vendedor (no pendientes de pago ni cancelados).
        /// La métrica pública suma <see cref="ItemOrden.Cantidad"/>: si un cliente compra 3, cuenta 3.
        /// </summary>
        public static IQueryable<ItemOrden> QueryItemsProductosVendidos(
            TiendaVirtualDbContext context, int vendedorId) =>
            context.ItemsOrden.Where(i =>
                i.Suborden.VendedorId == vendedorId &&
                i.Suborden.Estado != TipoEstadoSuborden.Cancelada &&
                i.Suborden.Orden.Estado != TipoEstadoOrden.PendientePago &&
                i.Suborden.Orden.Estado != TipoEstadoOrden.Cancelada);

        /// <summary>
        /// Total de unidades vendidas (suma de cantidades en líneas de pedido pagadas).
        /// </summary>
        public static async Task<int> ContarProductosVendidosAsync(
            TiendaVirtualDbContext context, int vendedorId)
        {
            var total = await QueryItemsProductosVendidos(context, vendedorId)
                .AsNoTracking()
                .SumAsync(i => (int?)i.Cantidad);

            return total ?? 0;
        }

        /// <summary>Alias histórico → unidades vendidas.</summary>
        public static Task<int> ContarVentasEntregadasAsync(
            TiendaVirtualDbContext context, int vendedorId) =>
            ContarProductosVendidosAsync(context, vendedorId);

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
