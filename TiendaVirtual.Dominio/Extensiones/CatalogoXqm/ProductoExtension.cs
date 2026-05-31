using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Dominio.Extensiones.CatalogoXqm
{
    public static class ProductoExtension
    {
        public static Producto ToEntidad(this ProductoDto dto)
        {
            if (dto == null)
                return null!;

            var producto = new Producto();

            producto.ProductoId = dto.ProductoId;
            producto.VendedorId = dto.VendedorId;
            producto.CategoriaId = dto.CategoriaId;
            producto.Nombre = dto.Nombre.Normalizar();
            producto.Slug = dto.Slug.Normalizar();
            producto.Descripcion = dto.Descripcion?.Normalizar_null();
            producto.DescripcionCorta = dto.DescripcionCorta?.Normalizar_null();
            producto.Material = dto.Material?.Normalizar_null();
            producto.Dimensiones = dto.Dimensiones?.Normalizar_null();
            producto.TieneVariantes = dto.TieneVariantes;
            producto.PrecioBase = dto.PrecioBase;
            producto.DiasElaboracion = dto.DiasElaboracion;
            producto.Estado = (TipoEstadoProducto)dto.Estado.Id;
            producto.Tipo = (TipoProducto)dto.Tipo.Id;
            producto.ArchivoPatronUrl = dto.ArchivoPatronUrl?.Normalizar_null();
            producto.Vistas = dto.Vistas;
            producto.Ventas = dto.Ventas;
            producto.CalificacionPromedio = dto.CalificacionPromedio;
            producto.TotalResenas = dto.TotalResenas;

            return producto;
        }

        public static ProductoDto ToDto(this Producto entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new ProductoDto();

            dto.ProductoId = entidad.ProductoId;
            dto.VendedorId = entidad.VendedorId;
            dto.CategoriaId = entidad.CategoriaId;
            dto.Nombre = entidad.Nombre;
            dto.Slug = entidad.Slug;
            dto.Descripcion = entidad.Descripcion;
            dto.DescripcionCorta = entidad.DescripcionCorta;
            dto.Material = entidad.Material;
            dto.Dimensiones = entidad.Dimensiones;
            dto.TieneVariantes = entidad.TieneVariantes;
            dto.PrecioBase = entidad.PrecioBase;
            dto.DiasElaboracion = entidad.DiasElaboracion;
            dto.Estado = new EnumeracionDto
            {
                Id = (int)entidad.Estado,
                Nombre = entidad.Estado.GetDescription()
            };
            dto.Tipo = new EnumeracionDto
            {
                Id = (int)entidad.Tipo,
                Nombre = entidad.Tipo.GetDescription()
            };
            dto.ArchivoPatronUrl = entidad.ArchivoPatronUrl;
            dto.Vistas = entidad.Vistas;
            dto.Ventas = entidad.Ventas;
            dto.CalificacionPromedio = entidad.CalificacionPromedio;
            dto.TotalResenas = entidad.TotalResenas;

            return dto;
        }
    }
}
