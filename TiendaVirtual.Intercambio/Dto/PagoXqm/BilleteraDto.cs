using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.PagoXqm
{
    public class BilleteraDto
    {
        public int VendedorId { get; set; }
        public decimal SaldoDisponible { get; set; }
        public decimal SaldoPendiente { get; set; }
        public decimal TotalGanado { get; set; }
        public decimal TotalRetirado { get; set; }
    }
}
