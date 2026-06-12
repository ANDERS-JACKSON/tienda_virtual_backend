using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    public class VendedorAdminListadoDto
    {
        public int VendedorId { get; set; }
        public string NombreTienda { get; set; } = null!;
        public string SlugTienda { get; set; } = null!;
        public string CorreoUsuario { get; set; } = null!;
        public EnumeracionDto Estado { get; set; } = null!;
        public string? PlanActual { get; set; }
        public EnumeracionDto? EstadoSuscripcion { get; set; }
        public int TotalProductos { get; set; }
        public int TotalVentas { get; set; }
        public decimal CalificacionPromedio { get; set; }
        public DateTime FechaRegistro { get; set; }
    }

    public class VendedorAdminDetalleDto : VendedorAdminListadoDto
    {
        public string? LogoUrl { get; set; }
        public string? BannerUrl { get; set; }
        public string? Biografia { get; set; }
        public bool VendePatrones { get; set; }
        public decimal MontoVendidoTotal { get; set; }
        public decimal ComisionGenerada { get; set; }
        public int ReclamosAbiertos { get; set; }
        public DateTime? FechaUltimaVenta { get; set; }
    }

    public class SuspenderVendedorDto
    {
        public string Motivo { get; set; } = null!;
    }
}
