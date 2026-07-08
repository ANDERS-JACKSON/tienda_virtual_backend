using System;

namespace TiendaVirtual.Intercambio.Dto.CatalogoXqm
{
    /// <summary>
    /// Versión pública de <see cref="VarianteProductoDto"/> para exponer al comprador
    /// (endpoints de catálogo y detalle de producto).
    ///
    /// Regla de negocio: el comprador NO debe ver la cantidad exacta de stock ni
    /// el inventario interno (reservado, umbral). Solo si la variante tiene stock
    /// para permitirle agregarla al carrito. La cantidad real y demás datos
    /// operativos siguen expuestos vía <see cref="VarianteProductoDto"/> únicamente
    /// en endpoints privados (vendedor/admin).
    /// </summary>
    public class VarianteProductoPublicoDto
    {
        public int VarianteId { get; set; }
        public int ProductoId { get; set; }
        public string? Sku { get; set; }
        public string? Nombre { get; set; }
        public decimal Precio { get; set; }
        public int? PesoGramos { get; set; }
        public string? Atributos { get; set; }
        public bool Activa { get; set; }

        /// <summary>
        /// Indica si la variante está disponible para agregar al carrito.
        /// Reemplaza al campo <c>CantidadDisponible</c> del DTO privado para
        /// no filtrar información de inventario al público.
        /// </summary>
        public bool TieneStock { get; set; }
    }
}
