using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;

namespace TiendaVirtual.Dominio.Modelo.PagoXqm
{
    public class Billetera
    {
        public int VendedorId { get; set; }
        public decimal SaldoDisponible { get; set; }
        public decimal SaldoPendiente { get; set; }
        public decimal TotalGanado { get; set; }
        public decimal TotalRetirado { get; set; }

        public virtual Vendedor Vendedor { get; set; } = null!;
    }
}
