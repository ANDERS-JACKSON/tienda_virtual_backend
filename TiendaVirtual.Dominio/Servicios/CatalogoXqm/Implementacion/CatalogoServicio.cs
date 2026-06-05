using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Extensiones.CatalogoXqm;
using TiendaVirtual.Dominio.Extensiones.VendedorXqm;
using TiendaVirtual.Dominio.Servicios.SuscripcionXqm.Implementacion;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Dominio.Servicios.CatalogoXqm.Implementacion
{
    public class CatalogoServicio : ICatalogoServicio
    {
        protected readonly TiendaVirtualDbContext _context;

        public CatalogoServicio(TiendaVirtualDbContext context) => _context = context;

        // ─────────────────────────────────────────────────────
        // Listado público con filtros y paginación "cargar más"
        // ─────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<PaginacionRespuestaDto<ProductoListadoDto>>> ListarAsync(
            FiltrosCatalogoDto filtros)
        {
            try
            {
                filtros.Pagina = Math.Max(1, filtros.Pagina);
                filtros.TamanioPagina = Math.Clamp(filtros.TamanioPagina, 1, 48);
                var now = DateTime.UtcNow;

                var query = _context.Productos
                    .AsNoTracking()
                    .Include(p => p.Vendedor)
                    .Include(p => p.Categoria)
                    .Include(p => p.Imagenes)
                    .Include(p => p.Variantes).ThenInclude(v => v.Stock)
                    .Where(p => p.Estado == TipoEstadoProducto.Activo &&
                                p.Vendedor.Estado == TipoEstadoVendedor.Activo)
                    .DondeVendedorTienePlanActivo(_context, now);

                // Filtro por categoría (incluye subcategorías)
                if (filtros.CategoriaId.HasValue)
                {
                    var ids = await ObtenerCategoriaConDescendientesAsync(filtros.CategoriaId.Value);
                    query = query.Where(p => ids.Contains(p.CategoriaId));
                }

                if (filtros.VendedorId.HasValue)
                    query = query.Where(p => p.VendedorId == filtros.VendedorId);

                if (!string.IsNullOrWhiteSpace(filtros.Busqueda))
                {
                    var b = filtros.Busqueda.Trim().ToLower();
                    query = query.Where(p =>
                        p.Nombre.ToLower().Contains(b) ||
                        (p.DescripcionCorta != null && p.DescripcionCorta.ToLower().Contains(b)) ||
                        p.Vendedor.NombreTienda.ToLower().Contains(b));
                }

                if (filtros.PrecioMin.HasValue)
                    query = query.Where(p => p.PrecioBase >= filtros.PrecioMin);

                if (filtros.PrecioMax.HasValue)
                    query = query.Where(p => p.PrecioBase <= filtros.PrecioMax);

                if (filtros.TipoProducto.HasValue)
                    query = query.Where(p => (int)p.Tipo == filtros.TipoProducto);

                if (filtros.SoloConOferta == true)
                {
                    query = query.Where(p => p.Ofertas.Any(o =>
                        o.Activa && o.FechaInicio <= now && o.FechaFin >= now));
                }

                // Ordenamiento
                query = filtros.OrdenarPor switch
                {
                    "precio_asc" => query.OrderBy(p => p.PrecioBase),
                    "precio_desc" => query.OrderByDescending(p => p.PrecioBase),
                    "mas_vendidos" => query.OrderByDescending(p => p.Ventas),
                    "mejor_calificados" => query.OrderByDescending(p => p.CalificacionPromedio)
                                                .ThenByDescending(p => p.TotalResenas),
                    _ => query.OrderByDescending(p => p.ProductoId) // novedades por defecto
                };

                var total = await query.CountAsync();
                var productos = await query
                    .Skip((filtros.Pagina - 1) * filtros.TamanioPagina)
                    .Take(filtros.TamanioPagina)
                    .ToListAsync();

                // Cargar ofertas vigentes en una sola query (para evitar N+1)
                var productosIds = productos.Select(p => p.ProductoId).ToList();
                var ofertasVigentes = await ObtenerOfertasVigentesAsync(productosIds);

                var items = productos.Select(p => MapearAListadoDto(p, ofertasVigentes)).ToList();

                var respuesta = new PaginacionRespuestaDto<ProductoListadoDto>
                {
                    Items = items,
                    Pagina = filtros.Pagina,
                    TamanioPagina = filtros.TamanioPagina,
                    TotalRegistros = total,
                    HayMas = filtros.Pagina * filtros.TamanioPagina < total
                };

                return ResultadoOperacion<PaginacionRespuestaDto<ProductoListadoDto>>.SetExito(respuesta);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<PaginacionRespuestaDto<ProductoListadoDto>>.SetError("Error: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────
        // Detalle del producto (incrementa contador de vistas)
        // ─────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<ProductoDetalleDto>> ObtenerPorSlugAsync(string slug)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(slug))
                    return ResultadoOperacion<ProductoDetalleDto>.SetError("Slug requerido.");

                var producto = await _context.Productos
                    .Include(p => p.Vendedor)
                    .Include(p => p.Categoria)
                    .Include(p => p.Variantes.Where(v => v.Activa)).ThenInclude(v => v.Stock)
                    .Include(p => p.Imagenes)
                    .Include(p => p.Ofertas)
                    .FirstOrDefaultAsync(p => p.Slug == slug && p.Estado == TipoEstadoProducto.Activo);

                if (producto == null)
                    return ResultadoOperacion<ProductoDetalleDto>.SetError("Producto no encontrado.");

                if (producto.Vendedor.Estado != TipoEstadoVendedor.Activo)
                    return ResultadoOperacion<ProductoDetalleDto>.SetError("Esta tienda no está disponible.");

                var nowDetalle = DateTime.UtcNow;
                var tiendaActiva = await _context.Suscripciones.AnyAsync(s =>
                    s.VendedorId == producto.VendedorId &&
                    ((s.Estado == TipoEstadoSuscripcion.EnPrueba &&
                      s.PruebaTerminaEn.HasValue &&
                      s.PruebaTerminaEn > nowDetalle) ||
                     (s.Estado == TipoEstadoSuscripcion.Activa &&
                      (!s.PeriodoFin.HasValue || s.PeriodoFin > nowDetalle))));
                if (!tiendaActiva)
                    return ResultadoOperacion<ProductoDetalleDto>.SetError("Esta tienda no está disponible.");

                // Incrementar vistas (fire-and-forget de cara al usuario; await para consistencia simple)
                producto.Vistas++;
                await _context.SaveChangesAsync();

                var now = DateTime.UtcNow;
                var ofertaVigente = producto.Ofertas
                    .Where(o => o.Activa && o.FechaInicio <= now && o.FechaFin >= now)
                    .OrderByDescending(o => o.OfertaId)
                    .FirstOrDefault();

                var tieneStock = producto.Tipo == TipoProducto.Patron ||
                                 producto.Variantes.Any(v => v.Stock != null && v.Stock.CantidadDisponible > 0);

                var precioActual = ofertaVigente?.PrecioOferta ?? producto.PrecioBase ?? 0;
                decimal? precioAnterior = ofertaVigente != null ? producto.PrecioBase : null;

                var totalProductosVendedor = await _context.Productos.CountAsync(p =>
                    p.VendedorId == producto.VendedorId && p.Estado == TipoEstadoProducto.Activo);

                var detalle = new ProductoDetalleDto
                {
                    ProductoId = producto.ProductoId,
                    Nombre = producto.Nombre,
                    Slug = producto.Slug,
                    Descripcion = producto.Descripcion,
                    DescripcionCorta = producto.DescripcionCorta,
                    Material = producto.Material,
                    Dimensiones = producto.Dimensiones,
                    TieneVariantes = producto.TieneVariantes,
                    PrecioBase = producto.PrecioBase,
                    DiasElaboracion = producto.DiasElaboracion,
                    Tipo = new EnumeracionDto { Id = (int)producto.Tipo, Nombre = producto.Tipo.ToString() },
                    Vistas = producto.Vistas,
                    Ventas = producto.Ventas,
                    CalificacionPromedio = producto.CalificacionPromedio,
                    TotalResenas = producto.TotalResenas,

                    Categoria = producto.Categoria.ToDto(),
                    Vendedor = producto.Vendedor.ToTiendaPublicaDto(totalProductosVendedor),
                    Variantes = producto.Variantes.Select(v => v.ToDto()).ToList(),
                    Imagenes = producto.Imagenes.OrderBy(i => i.Orden).Select(i => i.ToDto()).ToList(),
                    OfertaVigente = ofertaVigente?.ToDto(),

                    PrecioActual = precioActual,
                    PrecioAnterior = precioAnterior,
                    TieneStock = tieneStock
                };

                return ResultadoOperacion<ProductoDetalleDto>.SetExito(detalle);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<ProductoDetalleDto>.SetError("Error: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────
        // Productos relacionados (misma categoría)
        // ─────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<List<ProductoListadoDto>>> ObtenerRelacionadosAsync(
            string slug, int cantidad = 6)
        {
            try
            {
                var producto = await _context.Productos.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Slug == slug);
                if (producto == null)
                    return ResultadoOperacion<List<ProductoListadoDto>>.SetExito(new List<ProductoListadoDto>());

                var nowRelacionados = DateTime.UtcNow;
                var productos = await _context.Productos
                    .AsNoTracking()
                    .Include(p => p.Vendedor)
                    .Include(p => p.Categoria)
                    .Include(p => p.Imagenes)
                    .Include(p => p.Variantes).ThenInclude(v => v.Stock)
                    .Where(p => p.CategoriaId == producto.CategoriaId &&
                                p.ProductoId != producto.ProductoId &&
                                p.Estado == TipoEstadoProducto.Activo &&
                                p.Vendedor.Estado == TipoEstadoVendedor.Activo)
                    .DondeVendedorTienePlanActivo(_context, nowRelacionados)
                    .OrderByDescending(p => p.Ventas)
                    .Take(cantidad)
                    .ToListAsync();

                var ofertasVigentes = await ObtenerOfertasVigentesAsync(
                    productos.Select(p => p.ProductoId).ToList());

                var items = productos.Select(p => MapearAListadoDto(p, ofertasVigentes)).ToList();
                return ResultadoOperacion<List<ProductoListadoDto>>.SetExito(items);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<List<ProductoListadoDto>>.SetError("Error: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────
        // Productos públicos de un vendedor específico
        // ─────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<PaginacionRespuestaDto<ProductoListadoDto>>> ListarPorVendedorAsync(
            int vendedorId, int pagina, int tamanioPagina)
        {
            return await ListarAsync(new FiltrosCatalogoDto
            {
                VendedorId = vendedorId,
                Pagina = pagina,
                TamanioPagina = tamanioPagina,
                OrdenarPor = "mas_vendidos"
            });
        }

        // ─────────────────────────────────────────────────────
        // HELPERS PRIVADOS
        // ─────────────────────────────────────────────────────
        private async Task<List<int>> ObtenerCategoriaConDescendientesAsync(int categoriaId)
        {
            var todas = await _context.Categorias.AsNoTracking()
                .Select(c => new { c.CategoriaId, c.CategoriaPadreId })
                .ToListAsync();

            var resultado = new List<int> { categoriaId };
            var pendientes = new Queue<int>(new[] { categoriaId });
            while (pendientes.Count > 0)
            {
                var actual = pendientes.Dequeue();
                var hijos = todas.Where(c => c.CategoriaPadreId == actual).Select(c => c.CategoriaId).ToList();
                foreach (var h in hijos)
                {
                    if (!resultado.Contains(h))
                    {
                        resultado.Add(h);
                        pendientes.Enqueue(h);
                    }
                }
            }
            return resultado;
        }

        private async Task<Dictionary<int, Oferta>> ObtenerOfertasVigentesAsync(List<int> productosIds)
        {
            if (productosIds.Count == 0) return new Dictionary<int, Oferta>();
            var now = DateTime.UtcNow;
            var ofertas = await _context.Ofertas
                .AsNoTracking()
                .Where(o => productosIds.Contains(o.ProductoId) &&
                            o.Activa && o.FechaInicio <= now && o.FechaFin >= now)
                .OrderByDescending(o => o.OfertaId)
                .ToListAsync();
            return ofertas.GroupBy(o => o.ProductoId).ToDictionary(g => g.Key, g => g.First());
        }

        private static ProductoListadoDto MapearAListadoDto(Producto p, Dictionary<int, Oferta> ofertas)
        {
            var imgPrincipal = p.Imagenes.FirstOrDefault(i => i.EsPrincipal)?.Url
                             ?? p.Imagenes.OrderBy(i => i.Orden).FirstOrDefault()?.Url;

            ofertas.TryGetValue(p.ProductoId, out var oferta);
            var tieneStock = p.Tipo == TipoProducto.Patron ||
                             p.Variantes.Any(v => v.Stock != null && v.Stock.CantidadDisponible > 0);

            return new ProductoListadoDto
            {
                ProductoId = p.ProductoId,
                Nombre = p.Nombre,
                Slug = p.Slug,
                DescripcionCorta = p.DescripcionCorta,
                ImagenPrincipalUrl = imgPrincipal,
                VendedorId = p.VendedorId,
                NombreTienda = p.Vendedor.NombreTienda,
                SlugTienda = p.Vendedor.SlugTienda,
                CategoriaId = p.CategoriaId,
                NombreCategoria = p.Categoria.Nombre,
                PrecioBase = p.PrecioBase ?? 0,
                PrecioOferta = oferta?.PrecioOferta,
                PorcentajeDescuento = oferta?.PorcentajeDescuento,
                TieneOferta = oferta != null,
                Tipo = new EnumeracionDto { Id = (int)p.Tipo, Nombre = p.Tipo.ToString() },
                CalificacionPromedio = p.CalificacionPromedio,
                TotalResenas = p.TotalResenas,
                TieneStock = tieneStock
            };
        }
    }
}
