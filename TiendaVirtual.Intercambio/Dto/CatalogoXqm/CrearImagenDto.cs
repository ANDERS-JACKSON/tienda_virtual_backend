using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.CatalogoXqm
{
    public class CrearImagenDto
    {
        /// <summary>public_id de CDN (ej. tiendavirtual/abc) o URL absoluta legacy.</summary>
        [Required]
        [MaxLength(500)]
        public string Url { get; set; } = null!;

        [MaxLength(250)]
        public string? TextoAlt { get; set; }

        public int Orden { get; set; }
        public bool EsPrincipal { get; set; }
    }
}
