using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;

namespace TiendaVirtual.Dominio.Modelo.VentaXqm
{
    public class ItemCarrito
    {
        public int ItemCarritoId { get; set; }
        public int CarritoId { get; set; }
        public int VarianteId { get; set; }
        public int Cantidad { get; set; }
        public DateTime FechaAgregado { get; set; }

        public virtual Carrito Carrito { get; set; } = null!;
        public virtual VarianteProducto Variante { get; set; } = null!;
    }
}
