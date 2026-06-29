namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    /// <summary>Tarjeta de historia en el listado público (/historias).</summary>
    public class HistoriaPublicaListadoDto
    {
        public int VendedorId { get; set; }
        public string NombreTienda { get; set; } = null!;
        public string SlugTienda { get; set; } = null!;
        public string BiografiaResumen { get; set; } = null!;
        public bool TieneTextoCompleto { get; set; }
        public string? ImagenUrl { get; set; }
        public string? CategoriaPrincipal { get; set; }
    }

    /// <summary>Detalle de historia de un artesano (/historias/{slug}).</summary>
    public class HistoriaPublicaDetalleDto
    {
        public int VendedorId { get; set; }
        public string NombreTienda { get; set; } = null!;
        public string SlugTienda { get; set; } = null!;
        public string Biografia { get; set; } = null!;
        public string? LogoUrl { get; set; }
        public string? BannerUrl { get; set; }
        public decimal CalificacionPromedio { get; set; }
        public int TotalVentas { get; set; }
        public int TotalProductos { get; set; }
        public bool VendePatrones { get; set; }
        public string? CategoriaPrincipal { get; set; }
    }
}
