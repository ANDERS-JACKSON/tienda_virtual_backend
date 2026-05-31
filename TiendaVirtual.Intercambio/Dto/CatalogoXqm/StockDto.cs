using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.CatalogoXqm
{
    public class StockDto
    {
        public int VarianteId { get; set; }
        public int CantidadDisponible { get; set; }
        public int CantidadReservada { get; set; }
        public int UmbralStockBajo { get; set; }
    }
}
