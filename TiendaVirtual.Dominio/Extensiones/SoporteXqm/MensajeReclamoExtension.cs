using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.SoporteXqm;
using TiendaVirtual.Intercambio.Dto.SoporteXqm;

namespace TiendaVirtual.Dominio.Extensiones.SoporteXqm
{
    public static class MensajeReclamoExtension
    {
        public static MensajeReclamo ToEntidad(this MensajeReclamoDto dto)
        {
            if (dto == null)
                return null!;

            var mensaje = new MensajeReclamo();

            mensaje.MensajeId = dto.MensajeId;
            mensaje.ReclamoId = dto.ReclamoId;
            mensaje.RemitenteId = dto.RemitenteId;
            mensaje.Mensaje = dto.Mensaje.Normalizar();
            mensaje.Adjuntos = dto.Adjuntos?.Normalizar_null();
            mensaje.Fecha = dto.Fecha;

            return mensaje;
        }

        public static MensajeReclamoDto ToDto(this MensajeReclamo entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new MensajeReclamoDto();

            dto.MensajeId = entidad.MensajeId;
            dto.ReclamoId = entidad.ReclamoId;
            dto.RemitenteId = entidad.RemitenteId;
            dto.Mensaje = entidad.Mensaje;
            dto.Adjuntos = entidad.Adjuntos;
            dto.Fecha = entidad.Fecha;

            return dto;
        }
    }
}
