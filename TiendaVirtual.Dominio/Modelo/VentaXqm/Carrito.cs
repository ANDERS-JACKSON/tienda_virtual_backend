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
        public int UsuarioId { get; set; }

        public virtual Usuario Usuario { get; set; } = null!;
        public virtual ICollection<ItemCarrito> Items { get; set; } = new List<ItemCarrito>();
    }
}
