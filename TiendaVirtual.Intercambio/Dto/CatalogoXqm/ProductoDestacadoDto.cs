using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.CatalogoXqm
{
    public class ProductoDestacadoPublicoDto
    {
        public int ProductoDestacadoId { get; set; }
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = "";
        public string Slug { get; set; } = "";
        public string? NombreTienda { get; set; }
        public decimal? PrecioBase { get; set; }
        public decimal? PrecioOferta { get; set; }
        public decimal? PorcentajeDescuento { get; set; }
        public string? ImagenPrincipalPublicId { get; set; }
        public int Orden { get; set; }
        public EnumeracionDto Tipo { get; set; } = null!;
        public decimal CalificacionPromedio { get; set; }
        public int TotalResenas { get; set; }
        public bool TieneStock { get; set; }
    }

    public class ProductoDestacadoAdminDto
    {
        public int ProductoDestacadoId { get; set; }
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = "";
        public string? NombreTienda { get; set; }
        public string? ImagenPrincipalPublicId { get; set; }
        public int Orden { get; set; }
        public EnumeracionDto EstadoProducto { get; set; } = null!;
        public bool OcultoPorAdmin { get; set; }
    }

    public class AgregarProductoDestacadoDto
    {
        public int ProductoId { get; set; }
    }

    public class ReordenarProductosDestacadosDto
    {
        public List<ReordenItemDto> Items { get; set; } = new();
    }

    public class ReordenItemDto
    {
        public int ProductoDestacadoId { get; set; }
        public int Orden { get; set; }
    }
}
