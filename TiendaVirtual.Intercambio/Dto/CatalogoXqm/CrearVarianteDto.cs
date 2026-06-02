using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.CatalogoXqm
{
    public class CrearVarianteDto
    {
        [MaxLength(100)]
        public string? Sku { get; set; }

        [MaxLength(200)]
        public string? Nombre { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0.")]
        public decimal Precio { get; set; }

        public int? PesoGramos { get; set; }
        public string? Atributos { get; set; }

        [Range(0, int.MaxValue)]
        public int CantidadInicial { get; set; }

        public int UmbralStockBajo { get; set; } = 5;
    }
}
