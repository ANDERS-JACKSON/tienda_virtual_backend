using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.VentaXqm
{
    public class ItemOrdenDto
    {
        public long ItemOrdenId { get; set; }
        public long SubordenId { get; set; }
        public int? VarianteId { get; set; }
        public int? ProductoId { get; set; }
        public string? Slug { get; set; }
        public string NombreProducto { get; set; } = null!;
        public string? NombreVariante { get; set; }
        public string? ImagenUrl { get; set; }
        public decimal PrecioUnitario { get; set; }
        public int Cantidad { get; set; }
        public decimal TotalLinea { get; set; }
        public EnumeracionDto TipoProducto { get; set; } = null!;
        public string? ArchivoPatronUrl { get; set; }
    }
}
