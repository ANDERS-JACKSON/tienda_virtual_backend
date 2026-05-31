using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;

namespace TiendaVirtual.Dominio.Extensiones.SeguridadXqm
{
    public static class RolExtension
    {
        public static Rol ToEntidad(this RolDto dto)
        {
            if (dto == null)
                return null!;

            var rol = new Rol();

            rol.RolId = dto.RolId;
            rol.Nombre = dto.Nombre.Normalizar();
            rol.Descripcion = dto.Descripcion?.Normalizar_null();

            return rol;
        }

        public static RolDto ToDto(this Rol entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new RolDto();

            dto.RolId = entidad.RolId;
            dto.Nombre = entidad.Nombre;
            dto.Descripcion = entidad.Descripcion;

            return dto;
        }
    }
}
