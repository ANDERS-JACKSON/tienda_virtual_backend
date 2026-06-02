using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.CatalogoXqm
{
    /// <summary>
    /// Solo actualiza los campos "generales" del producto. Variantes, imágenes
    /// y ofertas se administran en sus propios endpoints.
    /// </summary>
    public class ActualizarProductoDto
    {
        [Required]
        public int CategoriaId { get; set; }

        [Required]
        [MaxLength(250)]
        public string Nombre { get; set; } = null!;

        [MaxLength(5000)]
        public string? Descripcion { get; set; }

        [MaxLength(300)]
        public string? DescripcionCorta { get; set; }

        [MaxLength(200)]
        public string? Material { get; set; }

        [MaxLength(200)]
        public string? Dimensiones { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? PrecioBase { get; set; }

        public int? DiasElaboracion { get; set; }

        [Url]
        [MaxLength(500)]
        public string? ArchivoPatronUrl { get; set; }
    }
}
