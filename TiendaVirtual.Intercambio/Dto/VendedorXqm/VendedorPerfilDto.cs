using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    public class VendedorPerfilDto
    {
        public int VendedorId { get; set; }
        public string NombreTienda { get; set; } = null!;
        public string SlugTienda { get; set; } = null!;
        public string? Biografia { get; set; }
        public string? LogoUrl { get; set; }
        public string? BannerUrl { get; set; }
        public string? NumeroYape { get; set; }
        public string? NumeroWhatsapp { get; set; }
        public string? DistritoId { get; set; }
        public string? UbicacionDepartamento { get; set; }
        public string? UbicacionProvincia { get; set; }
        public string? UbicacionDistrito { get; set; }
        public EnumeracionDto Estado { get; set; } = null!;
        public decimal CalificacionPromedio { get; set; }
        public int TotalVentas { get; set; }
        public int TotalProductos { get; set; }
        public bool VendePatrones { get; set; }
    }
}
