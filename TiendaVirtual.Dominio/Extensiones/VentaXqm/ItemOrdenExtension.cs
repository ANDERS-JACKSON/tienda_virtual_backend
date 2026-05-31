using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.VentaXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Extensiones.VentaXqm
{
    public static class ItemOrdenExtension
    {
        public static ItemOrden ToEntidad(this ItemOrdenDto dto)
        {
            if (dto == null)
                return null!;

            var item = new ItemOrden();

            item.ItemOrdenId = dto.ItemOrdenId;
            item.SubordenId = dto.SubordenId;
            item.VarianteId = dto.VarianteId;
            item.NombreProducto = dto.NombreProducto.Normalizar();
            item.NombreVariante = dto.NombreVariante?.Normalizar_null();
            item.PrecioUnitario = dto.PrecioUnitario;
            item.Cantidad = dto.Cantidad;
            item.TotalLinea = dto.TotalLinea;
            item.ImagenUrl = dto.ImagenUrl?.Normalizar_null();
            item.TipoProducto = (TipoProducto)dto.TipoProducto.Id;
            item.ArchivoPatronUrl = dto.ArchivoPatronUrl?.Normalizar_null();

            return item;
        }

        public static ItemOrdenDto ToDto(this ItemOrden entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new ItemOrdenDto();

            dto.ItemOrdenId = entidad.ItemOrdenId;
            dto.SubordenId = entidad.SubordenId;
            dto.VarianteId = entidad.VarianteId;
            dto.NombreProducto = entidad.NombreProducto;
            dto.NombreVariante = entidad.NombreVariante;
            dto.PrecioUnitario = entidad.PrecioUnitario;
            dto.Cantidad = entidad.Cantidad;
            dto.TotalLinea = entidad.TotalLinea;
            dto.ImagenUrl = entidad.ImagenUrl;
            dto.TipoProducto = new EnumeracionDto
            {
                Id = (int)entidad.TipoProducto,
                Nombre = entidad.TipoProducto.GetDescription()
            };
            dto.ArchivoPatronUrl = entidad.ArchivoPatronUrl;

            return dto;
        }
    }
}
