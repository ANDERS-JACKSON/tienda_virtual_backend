using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.CatalogoXqm
{
    public class ActualizarOfertaDto
    {
        [MaxLength(100)]
        public string? Nombre { get; set; }

        [Range(1, 99)]
        public decimal? PorcentajeDescuento { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? PrecioOferta { get; set; }

        [Required]
        public DateTime FechaInicio { get; set; }

        [Required]
        public DateTime FechaFin { get; set; }
    }
}
