using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.CatalogoXqm
{
    public class OfertaDto
    {
        public int OfertaId { get; set; }
        public int ProductoId { get; set; }
        public string? Nombre { get; set; }
        public decimal? PorcentajeDescuento { get; set; }
        public decimal? PrecioOferta { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public bool Activa { get; set; }
    }
}
