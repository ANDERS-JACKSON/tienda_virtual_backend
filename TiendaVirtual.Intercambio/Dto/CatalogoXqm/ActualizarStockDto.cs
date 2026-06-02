using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.CatalogoXqm
{
    public class ActualizarStockDto
    {
        [Range(0, int.MaxValue, ErrorMessage = "La cantidad no puede ser negativa.")]
        public int CantidadDisponible { get; set; }

        [Range(0, int.MaxValue)]
        public int UmbralStockBajo { get; set; }
    }
}
