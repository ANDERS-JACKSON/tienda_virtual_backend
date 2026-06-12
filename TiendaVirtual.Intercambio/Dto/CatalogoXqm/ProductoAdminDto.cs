using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.CatalogoXqm
{
    public class ProductoAdminListadoDto
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string NombreTienda { get; set; } = null!;
        public int VendedorId { get; set; }
        public string NombreCategoria { get; set; } = null!;
        public decimal? PrecioBase { get; set; }
        public int? StockDisponible { get; set; }
        public string? ImagenPrincipalUrl { get; set; }
        public EnumeracionDto Estado { get; set; } = null!;
        public string? MotivoPausaAdmin { get; set; }
        public bool OcultoPorAdmin => !string.IsNullOrWhiteSpace(MotivoPausaAdmin);
    }

    public class OcultarProductoDto
    {
        public string Motivo { get; set; } = null!;
    }
}
