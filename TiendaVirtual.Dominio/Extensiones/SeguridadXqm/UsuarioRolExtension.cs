using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;

namespace TiendaVirtual.Dominio.Extensiones.SeguridadXqm
{
    public static class UsuarioRolExtension
    {
        public static UsuarioRol ToEntidad(this UsuarioRolDto dto)
        {
            if (dto == null)
                return null!;

            var usuarioRol = new UsuarioRol();

            usuarioRol.UsuarioId = dto.UsuarioId;
            usuarioRol.RolId = dto.RolId;

            return usuarioRol;
        }

        public static UsuarioRolDto ToDto(this UsuarioRol entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new UsuarioRolDto();

            dto.UsuarioId = entidad.UsuarioId;
            dto.RolId = entidad.RolId;

            return dto;
        }
    }
}
