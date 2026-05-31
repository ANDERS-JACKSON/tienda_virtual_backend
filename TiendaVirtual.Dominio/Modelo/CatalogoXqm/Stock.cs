using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Dominio.Modelo.CatalogoXqm
{
    public class Stock
    {
        public int VarianteId { get; set; }
        public int CantidadDisponible { get; set; }
        public int CantidadReservada { get; set; }
        public int UmbralStockBajo { get; set; }

        public virtual VarianteProducto Variante { get; set; } = null!;
    }
}
