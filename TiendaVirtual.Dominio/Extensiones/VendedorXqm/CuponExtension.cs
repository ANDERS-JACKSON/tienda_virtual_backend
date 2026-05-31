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
    public static class CuponExtension
    {
        public static Cupon ToEntidad(this CuponDto dto)
        {
            if (dto == null)
                return null!;

            var cupon = new Cupon();

            cupon.CuponId = dto.CuponId;
            cupon.Codigo = dto.Codigo.Normalizar();
            cupon.TipoDescuento = (TipoDescuentoCupon)dto.TipoDescuento.Id;
            cupon.ValorDescuento = dto.ValorDescuento;
            cupon.MesesGratis = dto.MesesGratis;
            cupon.UsosMaximos = dto.UsosMaximos;
            cupon.UsosRealizados = dto.UsosRealizados;
            cupon.ValidoHasta = dto.ValidoHasta;
            cupon.Activo = dto.Activo;

            return cupon;
        }

        public static CuponDto ToDto(this Cupon entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new CuponDto();

            dto.CuponId = entidad.CuponId;
            dto.Codigo = entidad.Codigo;
            dto.TipoDescuento = new EnumeracionDto
            {
                Id = (int)entidad.TipoDescuento,
                Nombre = entidad.TipoDescuento.GetDescription()
            };
            dto.ValorDescuento = entidad.ValorDescuento;
            dto.MesesGratis = entidad.MesesGratis;
            dto.UsosMaximos = entidad.UsosMaximos;
            dto.UsosRealizados = entidad.UsosRealizados;
            dto.ValidoHasta = entidad.ValidoHasta;
            dto.Activo = entidad.Activo;

            return dto;
        }
    }
}
