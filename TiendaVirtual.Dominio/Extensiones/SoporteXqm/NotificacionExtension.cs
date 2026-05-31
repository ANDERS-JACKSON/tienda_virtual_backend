using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.SoporteXqm;
using TiendaVirtual.Intercambio.Dto.SoporteXqm;

namespace TiendaVirtual.Dominio.Extensiones.SoporteXqm
{
    public static class NotificacionExtension
    {
        public static Notificacion ToEntidad(this NotificacionDto dto)
        {
            if (dto == null)
                return null!;

            var notificacion = new Notificacion();

            notificacion.NotificacionId = dto.NotificacionId;
            notificacion.UsuarioId = dto.UsuarioId;
            notificacion.Tipo = dto.Tipo.Normalizar();
            notificacion.Titulo = dto.Titulo.Normalizar();
            notificacion.Cuerpo = dto.Cuerpo?.Normalizar_null();
            notificacion.Datos = dto.Datos?.Normalizar_null();
            notificacion.Leida = dto.Leida;
            notificacion.Fecha = dto.Fecha;

            return notificacion;
        }

        public static NotificacionDto ToDto(this Notificacion entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new NotificacionDto();

            dto.NotificacionId = entidad.NotificacionId;
            dto.UsuarioId = entidad.UsuarioId;
            dto.Tipo = entidad.Tipo;
            dto.Titulo = entidad.Titulo;
            dto.Cuerpo = entidad.Cuerpo;
            dto.Datos = entidad.Datos;
            dto.Leida = entidad.Leida;
            dto.Fecha = entidad.Fecha;

            return dto;
        }
    }
}
