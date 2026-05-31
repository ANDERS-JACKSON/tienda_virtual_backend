using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.VentaXqm
{
    public class SubordenDto
    {
        public long SubordenId { get; set; }
        public long OrdenId { get; set; }
        public int VendedorId { get; set; }
        public string NumeroSuborden { get; set; } = null!;
        public decimal Subtotal { get; set; }
        public decimal MontoEnvio { get; set; }
        public decimal MontoComision { get; set; }
        public decimal MontoVendedor { get; set; }
        public EnumeracionDto Estado { get; set; } = null!;
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaEntrega { get; set; }
    }
}
