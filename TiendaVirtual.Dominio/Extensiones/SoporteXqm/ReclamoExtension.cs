using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.SoporteXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.SoporteXqm;

namespace TiendaVirtual.Dominio.Extensiones.SoporteXqm
{
    public static class ReclamoExtension
    {
        public static Reclamo ToEntidad(this ReclamoDto dto)
        {
            if (dto == null)
                return null!;

            var reclamo = new Reclamo();

            reclamo.ReclamoId = dto.ReclamoId;
            reclamo.SubordenId = dto.SubordenId;
            reclamo.AbiertoPor = dto.AbiertoPor;
            reclamo.Motivo = (TipoMotivoReclamo)dto.Motivo.Id;
            reclamo.Descripcion = dto.Descripcion?.Normalizar_null();
            reclamo.Evidencias = dto.Evidencias?.Normalizar_null();
            reclamo.Estado = (TipoEstadoReclamo)dto.Estado.Id;
            reclamo.NotasResolucion = dto.NotasResolucion?.Normalizar_null();
            reclamo.ResueltoPor = dto.ResueltoPor;
            reclamo.MontoReembolso = dto.MontoReembolso;
            reclamo.FechaApertura = dto.FechaApertura;
            reclamo.FechaResolucion = dto.FechaResolucion;

            return reclamo;
        }

        public static ReclamoDto ToDto(this Reclamo entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new ReclamoDto();

            dto.ReclamoId = entidad.ReclamoId;
            dto.SubordenId = entidad.SubordenId;
            dto.AbiertoPor = entidad.AbiertoPor;
            dto.Motivo = new EnumeracionDto
            {
                Id = (int)entidad.Motivo,
                Nombre = entidad.Motivo.GetDescription()
            };
            dto.Descripcion = entidad.Descripcion;
            dto.Evidencias = entidad.Evidencias;
            dto.Estado = new EnumeracionDto
            {
                Id = (int)entidad.Estado,
                Nombre = entidad.Estado.GetDescription()
            };
            dto.NotasResolucion = entidad.NotasResolucion;
            dto.ResueltoPor = entidad.ResueltoPor;
            dto.MontoReembolso = entidad.MontoReembolso;
            dto.FechaApertura = entidad.FechaApertura;
            dto.FechaResolucion = entidad.FechaResolucion;

            return dto;
        }
    }
}
