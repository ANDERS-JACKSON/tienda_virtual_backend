namespace TiendaVirtual.Intercambio.Dto.CatalogoXqm
{
    /// <summary>
    /// Versión pública de <see cref="VarianteProductoDto"/> para el comprador
    /// (catálogo y detalle). Expone la cantidad comprable para el stepper y
    /// validaciones de carrito. No incluye inventario interno (reservado, umbral).
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

        /// <summary>Unidades disponibles para compra (0 si agotado).</summary>
        public int CantidadDisponible { get; set; }

        /// <summary>Conveniencia: <c>CantidadDisponible &gt; 0</c> (patrones siempre true).</summary>
        public bool TieneStock { get; set; }
    }
}
