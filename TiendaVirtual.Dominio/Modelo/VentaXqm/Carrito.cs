using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;

namespace TiendaVirtual.Dominio.Modelo.VentaXqm
{
    public class Carrito
    {
        public int CarritoId { get; set; }
        public Guid UsuarioId { get; set; }
        public DateTime FechaActualizacion { get; set; }
        public int? CuponPedidoId { get; set; }

        public virtual Usuario Usuario { get; set; } = null!;
        public virtual CuponPedido? CuponPedido { get; set; }
        public virtual ICollection<ItemCarrito> Items { get; set; } = new List<ItemCarrito>();
    }
}
