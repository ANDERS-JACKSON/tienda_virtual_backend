using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.VentaXqm
{
    public class OrdenDto
    {
        public long OrdenId { get; set; }
        public string NumeroOrden { get; set; } = null!;
        public int ClienteId { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TotalEnvio { get; set; }
        public decimal TotalDescuento { get; set; }
        public decimal Total { get; set; }
        public string CorreoCliente { get; set; } = null!;
        public string? TelefonoCliente { get; set; }
        public string DireccionEnvio { get; set; } = null!;
        public EnumeracionDto Estado { get; set; } = null!;
        public DateTime Fecha { get; set; }
    }
}
