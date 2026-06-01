using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    public class PedidoVendedorDto
    {
        public long SubordenId { get; set; }
        public string NumeroSuborden { get; set; } = null!;
        public string NombreCliente { get; set; } = null!;
        public string CorreoCliente { get; set; } = null!;
        public decimal Subtotal { get; set; }
        public decimal MontoEnvio { get; set; }
        public decimal MontoVendedor { get; set; }
        public EnumeracionDto Estado { get; set; } = null!;
        public DateTime Fecha { get; set; }
        public int TotalItems { get; set; }
    }
}
