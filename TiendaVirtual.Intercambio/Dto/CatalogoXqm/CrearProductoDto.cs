using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.CatalogoXqm
{
    public class CrearProductoDto
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

        public bool TieneVariantes { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? PrecioBase { get; set; }

        public int? DiasElaboracion { get; set; }

        [Required]
        public EnumeracionDto Tipo { get; set; } = null!;   // FISICO=1, PATRON=2

        [Url]
        [MaxLength(500)]
        public string? ArchivoPatronUrl { get; set; }

        /// <summary>Solo se usa cuando TieneVariantes = false.</summary>
        public int CantidadInicial { get; set; }

        /// <summary>Solo se usa cuando TieneVariantes = false.</summary>
        public int UmbralStockBajo { get; set; } = 5;

        public List<CrearVarianteDto> Variantes { get; set; } = new();
        public List<CrearImagenDto> Imagenes { get; set; } = new();
    }
}
