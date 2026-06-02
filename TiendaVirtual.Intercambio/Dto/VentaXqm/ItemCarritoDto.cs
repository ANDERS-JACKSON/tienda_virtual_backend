using System.Text.Json.Serialization;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.VentaXqm
{
    public class ItemCarritoDto
    {
        public int ItemCarritoId { get; set; }
        public int VarianteId { get; set; }
        public int ProductoId { get; set; }
        public string Slug { get; set; } = null!;
        public string NombreProducto { get; set; } = null!;
        public string? NombreVariante { get; set; }
        public string? ImagenUrl { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal? PrecioOriginal { get; set; }
        public int Cantidad { get; set; }
        public int StockDisponible { get; set; }
        public decimal Subtotal { get; set; }
        public EnumeracionDto TipoProducto { get; set; } = null!;

        // Solo para agrupar en el backend; no se serializa.
        [JsonIgnore] public int VendedorIdInterno { get; set; }
        [JsonIgnore] public string NombreTiendaInterno { get; set; } = string.Empty;
        [JsonIgnore] public string SlugTiendaInterno { get; set; } = string.Empty;
    }
}
