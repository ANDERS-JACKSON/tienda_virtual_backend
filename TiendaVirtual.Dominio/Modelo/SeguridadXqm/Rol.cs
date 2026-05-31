using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Dominio.Modelo.SeguridadXqm
{
    public class Rol
    {
        public short RolId { get; set; }

        [Required]
        public string Nombre { get; set; } = null!;

        public string? Descripcion { get; set; }

        public virtual ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
    }
}
