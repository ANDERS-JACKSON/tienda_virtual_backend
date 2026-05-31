using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Dominio.Modelo.SeguridadXqm
{
    public class UsuarioRol
    {
        public int UsuarioId { get; set; }
        public short RolId { get; set; }

        public virtual Usuario Usuario { get; set; } = null!;
        public virtual Rol Rol { get; set; } = null!;
    }
}
