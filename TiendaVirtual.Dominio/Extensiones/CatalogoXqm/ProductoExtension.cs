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

        /// <summary>
        /// DTO completo para la vista del vendedor. Requiere navegaciones cargadas:
        /// Categoria, Variantes(.Stock), Imagenes, Ofertas.
        /// </summary>
        public static ProductoDto ToDto(this Producto p)
        {
            if (p == null) return null!;
            var now = DateTime.UtcNow;
            var ofertaVigente = p.Ofertas
                .Where(o => o.Activa && o.FechaInicio <= now && o.FechaFin >= now)
                .OrderByDescending(o => o.OfertaId)
                .FirstOrDefault();

            return new ProductoDto
            {
                ProductoId = p.ProductoId,
                VendedorId = p.VendedorId,
                CategoriaId = p.CategoriaId,
                NombreCategoria = p.Categoria?.Nombre ?? string.Empty,
                Nombre = p.Nombre,
                Slug = p.Slug,
                Descripcion = p.Descripcion,
                DescripcionCorta = p.DescripcionCorta,
                Material = p.Material,
                Dimensiones = p.Dimensiones,
                TieneVariantes = p.TieneVariantes,
                PrecioBase = p.PrecioBase,
                DiasElaboracion = p.DiasElaboracion,
                Estado = new EnumeracionDto { Id = (int)p.Estado, Nombre = p.Estado.ToString() },
                Tipo = new EnumeracionDto { Id = (int)p.Tipo, Nombre = p.Tipo.ToString() },
                ArchivoPatronUrl = p.ArchivoPatronUrl,
                Vistas = p.Vistas,
                Ventas = p.Ventas,
                CalificacionPromedio = p.CalificacionPromedio,
                TotalResenas = p.TotalResenas,
                Variantes = p.Variantes.Select(v => v.ToDto()).ToList(),
                Imagenes = p.Imagenes.OrderBy(i => i.Orden).Select(i => i.ToDto()).ToList(),
                OfertaVigente = ofertaVigente?.ToDto()
            };
        }
    }
}
