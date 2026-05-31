using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;

namespace TiendaVirtual.Dominio.Extensiones.VendedorXqm
{
    public static class SuscripcionExtension
    {
        public static Suscripcion ToEntidad(this SuscripcionDto dto)
        {
            if (dto == null)
                return null!;

            var suscripcion = new Suscripcion();

            suscripcion.SuscripcionId = dto.SuscripcionId;
            suscripcion.VendedorId = dto.VendedorId;
            suscripcion.PlanId = dto.PlanId;
            suscripcion.Estado = (TipoEstadoSuscripcion)dto.Estado.Id;
            suscripcion.MesesGratisOtorgados = dto.MesesGratisOtorgados;
            suscripcion.PrecioPersonalizado = dto.PrecioPersonalizado;
            suscripcion.CuponId = dto.CuponId;
            suscripcion.PruebaTerminaEn = dto.PruebaTerminaEn;
            suscripcion.PeriodoInicio = dto.PeriodoInicio;
            suscripcion.PeriodoFin = dto.PeriodoFin;

            return suscripcion;
        }

        public static SuscripcionDto ToDto(this Suscripcion entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new SuscripcionDto();

            dto.SuscripcionId = entidad.SuscripcionId;
            dto.VendedorId = entidad.VendedorId;
            dto.PlanId = entidad.PlanId;
            dto.Estado = new EnumeracionDto
            {
                Id = (int)entidad.Estado,
                Nombre = entidad.Estado.GetDescription()
            };
            dto.MesesGratisOtorgados = entidad.MesesGratisOtorgados;
            dto.PrecioPersonalizado = entidad.PrecioPersonalizado;
            dto.CuponId = entidad.CuponId;
            dto.PruebaTerminaEn = entidad.PruebaTerminaEn;
            dto.PeriodoInicio = entidad.PeriodoInicio;
            dto.PeriodoFin = entidad.PeriodoFin;

            return dto;
        }
    }
}
