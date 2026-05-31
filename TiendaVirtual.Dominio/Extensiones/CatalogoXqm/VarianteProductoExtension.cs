using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;

namespace TiendaVirtual.Dominio.Extensiones.CatalogoXqm
{
    public static class VarianteProductoExtension
    {
        public static VarianteProducto ToEntidad(this VarianteProductoDto dto)
        {
            if (dto == null)
                return null!;

            var variante = new VarianteProducto();

            variante.VarianteId = dto.VarianteId;
            variante.ProductoId = dto.ProductoId;
            variante.Sku = dto.Sku?.Normalizar_null();
            variante.Nombre = dto.Nombre?.Normalizar_null();
            variante.Precio = dto.Precio;
            variante.PesoGramos = dto.PesoGramos;
            variante.Atributos = dto.Atributos?.Normalizar_null();
            variante.Activa = dto.Activa;

            return variante;
        }

        public static VarianteProductoDto ToDto(this VarianteProducto entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new VarianteProductoDto();

            dto.VarianteId = entidad.VarianteId;
            dto.ProductoId = entidad.ProductoId;
            dto.Sku = entidad.Sku;
            dto.Nombre = entidad.Nombre;
            dto.Precio = entidad.Precio;
            dto.PesoGramos = entidad.PesoGramos;
            dto.Atributos = entidad.Atributos;
            dto.Activa = entidad.Activa;

            return dto;
        }
    }
}
