using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.VentaXqm
{
    public class ItemCarritoDto
    {
        public int ItemCarritoId { get; set; }
        public int CarritoId { get; set; }
        public int VarianteId { get; set; }
        public int Cantidad { get; set; }
    }
}
