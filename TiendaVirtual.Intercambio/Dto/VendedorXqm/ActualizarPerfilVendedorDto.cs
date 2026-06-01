using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    public class ActualizarPerfilVendedorDto
    {
        [Required]
        [MaxLength(150)]
        public string NombreTienda { get; set; } = null!;

        [MaxLength(1000)]
        public string? Biografia { get; set; }

        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        [MaxLength(500)]
        public string? BannerUrl { get; set; }

        [MaxLength(20)]
        public string? NumeroYape { get; set; }
    }
}
