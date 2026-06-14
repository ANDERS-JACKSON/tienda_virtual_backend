using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.CatalogoXqm
{
    public class ActualizarCategoriaDto
    {
        public int? CategoriaPadreId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; } = null!;

        [MaxLength(500)]
        public string? Descripcion { get; set; }

        /// <summary>public_id de CDN o URL absoluta legacy.</summary>
        [MaxLength(500)]
        public string? ImagenUrl { get; set; }

        public int Orden { get; set; }
        public bool Activa { get; set; }
    }
}
