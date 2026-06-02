using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.CatalogoXqm
{
    public class ActualizarVarianteDto
    {
        [MaxLength(100)]
        public string? Sku { get; set; }

        [MaxLength(200)]
        public string? Nombre { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Precio { get; set; }

        public int? PesoGramos { get; set; }
        public string? Atributos { get; set; }
        public bool Activa { get; set; }
    }
}
