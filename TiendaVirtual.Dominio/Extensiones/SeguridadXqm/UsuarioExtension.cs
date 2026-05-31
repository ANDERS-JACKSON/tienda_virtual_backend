using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Dominio.Extensiones.SeguridadXqm
{
    public static class UsuarioExtension
    {
        public static Usuario ToEntidad(this UsuarioDto dto)
        {
            if (dto == null)
                return null!;

            var usuario = new Usuario();

            usuario.UsuarioId = dto.UsuarioId;
            usuario.PersonaId = dto.PersonaId;
            usuario.Correo = dto.Correo.Normalizar();
            usuario.Contrasena = dto.Contrasena.Normalizar();
            usuario.CorreoConfirmado = dto.CorreoConfirmado;
            usuario.ForzarCambioClave = dto.ForzarCambioClave;
            usuario.Estado = (TipoEstadoUsuario)dto.Estado.Id;
            usuario.FechaAlta = dto.FechaAlta;
            usuario.UltimoAcceso = dto.UltimoAcceso;

            return usuario;
        }

        public static UsuarioDto ToDto(this Usuario entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new UsuarioDto();

            dto.UsuarioId = entidad.UsuarioId;
            dto.PersonaId = entidad.PersonaId;
            dto.Correo = entidad.Correo;
            dto.Contrasena = entidad.Contrasena;
            dto.CorreoConfirmado = entidad.CorreoConfirmado;
            dto.ForzarCambioClave = entidad.ForzarCambioClave;
            dto.Estado = new EnumeracionDto
            {
                Id = (int)entidad.Estado,
                Nombre = entidad.Estado.GetDescription()
            };
            dto.FechaAlta = entidad.FechaAlta;
            dto.UltimoAcceso = entidad.UltimoAcceso;

            return dto;
        }
    }
}
