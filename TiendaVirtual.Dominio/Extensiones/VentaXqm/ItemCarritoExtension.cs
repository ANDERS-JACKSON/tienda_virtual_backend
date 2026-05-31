using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.VentaXqm;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Extensiones.VentaXqm
{
    public static class ItemCarritoExtension
    {
        public static ItemCarrito ToEntidad(this ItemCarritoDto dto)
        {
            if (dto == null)
                return null!;

            var item = new ItemCarrito();

            item.ItemCarritoId = dto.ItemCarritoId;
            item.CarritoId = dto.CarritoId;
            item.VarianteId = dto.VarianteId;
            item.Cantidad = dto.Cantidad;

            return item;
        }

        public static ItemCarritoDto ToDto(this ItemCarrito entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new ItemCarritoDto();

            dto.ItemCarritoId = entidad.ItemCarritoId;
            dto.CarritoId = entidad.CarritoId;
            dto.VarianteId = entidad.VarianteId;
            dto.Cantidad = entidad.Cantidad;

            return dto;
        }
    }
}
