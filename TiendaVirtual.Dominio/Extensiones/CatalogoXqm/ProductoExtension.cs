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
        /// <summary>
        /// Devuelve la variante que se muestra por defecto en catálogo y detalle:
        /// la primera activa (menor VarianteId), es decir la que el vendedor creó primero.
        /// El comprador ve el mismo precio en catálogo y al abrir el detalle.
        /// </summary>
        public static VarianteProducto? ObtenerVariantePorDefecto(this Producto p)
        {
            if (p?.Variantes == null) return null;
            return p.Variantes
                .Where(v => v.Activa)
                .OrderBy(v => v.VarianteId)
                .FirstOrDefault();
        }

        /// <summary>
        /// Precio de referencia para catálogo: precio de la variante por defecto (la primera
        /// creada). Si no hay variantes activas, cae al precio base del producto.
        /// </summary>
        public static decimal ObtenerPrecioCatalogo(this Producto p)
        {
            var variante = p.ObtenerVariantePorDefecto();
            if (variante != null) return variante.Precio;
            return p?.PrecioBase ?? 0;
        }

        /// <summary>Precio mínimo entre variantes activas (útil para "Desde S/…").</summary>
        public static decimal ObtenerPrecioMinimo(this Producto p)
        {
            if (p?.Variantes == null) return p?.PrecioBase ?? 0;
            var activas = p.Variantes.Where(v => v.Activa).ToList();
            if (activas.Count > 0) return activas.Min(v => v.Precio);
            return p.PrecioBase ?? 0;
        }

        /// <summary>Indica si el comprador debe elegir entre variantes (2+ activas o flag explícito).</summary>
        public static bool TieneVariantesComprables(this Producto p)
        {
            if (p == null) return false;
            var activas = p.Variantes?.Count(v => v.Activa) ?? 0;
            return p.TieneVariantes || activas > 1;
        }

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
