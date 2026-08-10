using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;

namespace TiendaVirtual.Dominio.Extensiones.CatalogoXqm
{
    public static class VarianteProductoExtension
    {
        /// <summary>
        /// Mapea a DTO privado (vendedor / admin) con cantidades reales de stock.
        /// NO debe usarse en endpoints públicos.
        /// </summary>
        public static VarianteProductoDto ToDto(this VarianteProducto v)
        {
            if (v == null) return null!;
            return new VarianteProductoDto
            {
                VarianteId = v.VarianteId,
                ProductoId = v.ProductoId,
                Sku = v.Sku,
                Nombre = v.Nombre,
                Precio = v.Precio,
                PesoGramos = v.PesoGramos,
                Atributos = v.Atributos,
                Activa = v.Activa,
                CantidadDisponible = v.Stock?.CantidadDisponible ?? 0,
                CantidadReservada = v.Stock?.CantidadReservada ?? 0,
                UmbralStockBajo = v.Stock?.UmbralStockBajo ?? 5
            };
        }

        /// <summary>
        /// Mapea a DTO público para el comprador con cantidad comprable.
        /// Los patrones digitales se tratan como stock ilimitado (techo alto).
        /// </summary>
        public static VarianteProductoPublicoDto ToPublicDto(
            this VarianteProducto v, TipoProducto tipoProducto)
        {
            if (v == null) return null!;
            const int stockPatron = 9999;
            var cantidad = tipoProducto == TipoProducto.Patron
                ? stockPatron
                : (v.Stock?.CantidadDisponible ?? 0);
            return new VarianteProductoPublicoDto
            {
                VarianteId = v.VarianteId,
                ProductoId = v.ProductoId,
                Sku = v.Sku,
                Nombre = v.Nombre,
                Precio = v.Precio,
                PesoGramos = v.PesoGramos,
                Atributos = v.Atributos,
                Activa = v.Activa,
                CantidadDisponible = cantidad,
                TieneStock = cantidad > 0
            };
        }
    }
}
