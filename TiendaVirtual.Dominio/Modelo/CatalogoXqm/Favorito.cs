using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;

namespace TiendaVirtual.Dominio.Modelo.CatalogoXqm
{
    public class Favorito
    {
        public int UsuarioId { get; set; }
        public int ProductoId { get; set; }
        public DateTime Fecha { get; set; }

        public virtual Usuario Usuario { get; set; } = null!;
        public virtual Producto Producto { get; set; } = null!;
    }
}
