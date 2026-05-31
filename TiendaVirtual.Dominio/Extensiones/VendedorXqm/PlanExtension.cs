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
    public static class PlanExtension
    {
        public static Plan ToEntidad(this PlanDto dto)
        {
            if (dto == null)
                return null!;

            var plan = new Plan();

            plan.PlanId = dto.PlanId;
            plan.Codigo = dto.Codigo.Normalizar();
            plan.Nombre = dto.Nombre.Normalizar();
            plan.Descripcion = dto.Descripcion?.Normalizar_null();
            plan.Precio = dto.Precio;
            plan.Periodo = (TipoPeriodoPlan)dto.Periodo.Id;
            plan.MaxProductos = dto.MaxProductos;
            plan.TasaComision = dto.TasaComision;
            plan.Activo = dto.Activo;

            return plan;
        }

        public static PlanDto ToDto(this Plan entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new PlanDto();

            dto.PlanId = entidad.PlanId;
            dto.Codigo = entidad.Codigo;
            dto.Nombre = entidad.Nombre;
            dto.Descripcion = entidad.Descripcion;
            dto.Precio = entidad.Precio;
            dto.Periodo = new EnumeracionDto
            {
                Id = (int)entidad.Periodo,
                Nombre = entidad.Periodo.GetDescription()
            };
            dto.MaxProductos = entidad.MaxProductos;
            dto.TasaComision = entidad.TasaComision;
            dto.Activo = entidad.Activo;

            return dto;
        }
    }
}
