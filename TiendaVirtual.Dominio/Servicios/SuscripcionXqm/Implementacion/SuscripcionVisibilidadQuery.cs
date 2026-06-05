using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;

namespace TiendaVirtual.Dominio.Servicios.SuscripcionXqm.Implementacion
{
    /// <summary>Filtros EF para catálogo y tiendas públicas según suscripción comercial activa.</summary>
    internal static class SuscripcionVisibilidadQuery
    {
        public static IQueryable<Producto> DondeVendedorTienePlanActivo(
            this IQueryable<Producto> productos,
            TiendaVirtualDbContext context,
            DateTime now) =>
            productos.Where(p => context.Suscripciones.Any(s =>
                s.VendedorId == p.VendedorId &&
                ((s.Estado == TipoEstadoSuscripcion.EnPrueba &&
                  s.PruebaTerminaEn.HasValue &&
                  s.PruebaTerminaEn > now) ||
                 (s.Estado == TipoEstadoSuscripcion.Activa &&
                  (!s.PeriodoFin.HasValue || s.PeriodoFin > now)))));

        public static IQueryable<Vendedor> ConPlanActivo(
            this IQueryable<Vendedor> vendedores,
            TiendaVirtualDbContext context,
            DateTime now) =>
            vendedores.Where(v => context.Suscripciones.Any(s =>
                s.VendedorId == v.VendedorId &&
                ((s.Estado == TipoEstadoSuscripcion.EnPrueba &&
                  s.PruebaTerminaEn.HasValue &&
                  s.PruebaTerminaEn > now) ||
                 (s.Estado == TipoEstadoSuscripcion.Activa &&
                  (!s.PeriodoFin.HasValue || s.PeriodoFin > now)))));
    }
}
