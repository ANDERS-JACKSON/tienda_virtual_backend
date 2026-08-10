using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Extensiones.CatalogoXqm;
using TiendaVirtual.Dominio.Extensiones.VendedorXqm;
using TiendaVirtual.Dominio.Servicios.VendedorXqm;
using TiendaVirtual.Dominio.Servicios.SuscripcionXqm.Implementacion;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;
using TiendaVirtual.Dominio.Utilidad;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Dominio.Servicios.CatalogoXqm.Implementacion
{
    public class CatalogoServicio : ICatalogoServicio
    {
        protected readonly TiendaVirtualDbContext _context;
        private readonly ILogger<CatalogoServicio> _logger;
        private readonly IMemoryCache _cache;

        public CatalogoServicio(
            TiendaVirtualDbContext context,
            ILogger<CatalogoServicio> logger,
            IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

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
                    .AsSplitQuery()
                    .AsNoTracking()
                    .Include(p => p.Vendedor)
                    .Include(p => p.Categoria)
                    .Include(p => p.Imagenes)
                    .Include(p => p.Variantes).ThenInclude(v => v.Stock)
                    .Where(p => p.Estado == TipoEstadoProducto.Activo &&
                                p.Vendedor.Estado == TipoEstadoVendedor.Activo)
                    .DondeVendedorTienePlanActivo(_context, now);

                // Filtro por categoría(s): cada id incluye subcategorías (OR)
                var idsCategoriaSolicitados = new List<int>();
                if (filtros.CategoriaIds is { Count: > 0 })
                    idsCategoriaSolicitados.AddRange(filtros.CategoriaIds);
                else if (filtros.CategoriaId.HasValue)
                    idsCategoriaSolicitados.Add(filtros.CategoriaId.Value);

                if (idsCategoriaSolicitados.Count > 0)
                {
                    var ids = await ObtenerCategoriasConDescendientesAsync(idsCategoriaSolicitados);
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
                {
                    var min = filtros.PrecioMin.Value;
                    query = query.Where(p =>
                        (p.Variantes.Any(v => v.Activa)
                            ? p.Variantes.Where(v => v.Activa).Min(v => v.Precio)
                            : (p.PrecioBase ?? 0)) >= min);
                }

                if (filtros.PrecioMax.HasValue)
                {
                    var max = filtros.PrecioMax.Value;
                    query = query.Where(p =>
                        (p.Variantes.Any(v => v.Activa)
                            ? p.Variantes.Where(v => v.Activa).Min(v => v.Precio)
                            : (p.PrecioBase ?? 0)) <= max);
                }

                if (filtros.TipoProducto.HasValue)
                    query = query.Where(p => (int)p.Tipo == filtros.TipoProducto);

                if (filtros.SoloConOferta == true)
                {
                    query = query.Where(p => p.Ofertas.Any(o =>
                        o.Activa && o.FechaInicio <= now && o.FechaFin >= now));
                }

                // Precio efectivo = min variante activa o PrecioBase (mismo criterio que el filtro)
                query = filtros.OrdenarPor switch
                {
                    "precio_asc" => query.OrderBy(p =>
                        p.Variantes.Any(v => v.Activa)
                            ? p.Variantes.Where(v => v.Activa).Min(v => v.Precio)
                            : (p.PrecioBase ?? 0)),
                    "precio_desc" => query.OrderByDescending(p =>
                        p.Variantes.Any(v => v.Activa)
                            ? p.Variantes.Where(v => v.Activa).Min(v => v.Precio)
                            : (p.PrecioBase ?? 0)),
                    "mas_vendidos" => query.OrderByDescending(p => p.Ventas),
                    "mejor_calificados" => query.OrderByDescending(p => p.CalificacionPromedio)
                                                .ThenByDescending(p => p.TotalResenas),
                    "relevancia" => query.OrderByDescending(p => p.Ventas)
                                         .ThenByDescending(p => p.CalificacionPromedio)
                                         .ThenByDescending(p => p.ProductoId),
                    _ => query.OrderByDescending(p => p.ProductoId) // novedades
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
                _logger.LogError(ex, "Error en CatalogoServicio.ListarAsync");
                return ResultadoOperacion<PaginacionRespuestaDto<ProductoListadoDto>>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
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
                    .AsSplitQuery()
                    .Include(p => p.Vendedor)
                    .Include(p => p.Categoria)
                    .Include(p => p.Variantes).ThenInclude(v => v.Stock)
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

                await _context.Productos
                    .Where(p => p.ProductoId == producto.ProductoId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Vistas, p => p.Vistas + 1));

                var now = DateTime.UtcNow;
                var ofertaVigente = producto.Ofertas
                    .Where(o => o.Activa && o.FechaInicio <= now && o.FechaFin >= now)
                    .OrderByDescending(o => o.OfertaId)
                    .FirstOrDefault();

                var tieneStock = producto.Tipo == TipoProducto.Patron ||
                                 producto.Variantes.Any(v => v.Stock != null && v.Stock.CantidadDisponible > 0);

                // Detalle: usa la variante por defecto (primera creada) para el precio mostrado
                // al cargar. El frontend recalcula al cambiar de variante.
                var variantePorDefecto = producto.ObtenerVariantePorDefecto();
                var precioVariantePorDefecto = variantePorDefecto?.Precio ?? producto.PrecioBase ?? 0;
                var esProductoSinVariantesReales = !producto.Variantes.Any(v => v.Activa)
                    || producto.Variantes.Count(v => v.Activa) == 1;
                var precioCalc = PrecioOfertaUtil.Calcular(
                    precioVariantePorDefecto, ofertaVigente, esProductoSinVariantesReales);
                var precioActual = precioCalc.PrecioActual;
                decimal? precioAnterior = precioCalc.TieneDescuento ? precioCalc.PrecioOriginal : null;

                var totalProductosVendedor = await _context.Productos.CountAsync(p =>
                    p.VendedorId == producto.VendedorId && p.Estado == TipoEstadoProducto.Activo);
                var totalVentasVendedor = await VendedorMetricasHelper.ContarVentasEntregadasAsync(
                    _context, producto.VendedorId);

                var detalle = new ProductoDetalleDto
                {
                    ProductoId = producto.ProductoId,
                    Nombre = producto.Nombre,
                    Slug = producto.Slug,
                    Descripcion = producto.Descripcion,
                    DescripcionCorta = producto.DescripcionCorta,
                    Material = producto.Material,
                    Dimensiones = producto.Dimensiones,
                    TieneVariantes = producto.TieneVariantesComprables(),
                    PrecioBase = producto.PrecioBase,
                    DiasElaboracion = producto.DiasElaboracion,
                    Tipo = new EnumeracionDto { Id = (int)producto.Tipo, Nombre = producto.Tipo.ToString() },
                    Vistas = producto.Vistas,
                    Ventas = producto.Ventas,
                    CalificacionPromedio = producto.CalificacionPromedio,
                    TotalResenas = producto.TotalResenas,

                    Categoria = producto.Categoria.ToDto(),
                    Vendedor = producto.Vendedor.ToTiendaPublicaDto(totalProductosVendedor, totalVentasVendedor),
                    Variantes = producto.Variantes.Where(v => v.Activa)
                        .Select(v => v.ToPublicDto(producto.Tipo))
                        .ToList(),
                    Imagenes = producto.Imagenes.OrderBy(i => i.Orden).Select(i => i.ToDto()).ToList(),
                    OfertaVigente = ofertaVigente?.ToDto(),

                    PrecioActual = precioActual,
                    PrecioAnterior = precioAnterior,
                    VarianteIdDefecto = variantePorDefecto?.VarianteId,
                    TieneStock = tieneStock
                };

                return ResultadoOperacion<ProductoDetalleDto>.SetExito(detalle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CatalogoServicio.ObtenerPorSlugAsync");
                return ResultadoOperacion<ProductoDetalleDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        // ─────────────────────────────────────────────────────
        // Productos relacionados (misma categoría y mismo tipo)
        // ─────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<List<ProductoListadoDto>>> ObtenerRelacionadosAsync(
            string slug, int cantidad = 6)
        {
            try
            {
                cantidad = Math.Clamp(cantidad, 1, 24);

                var producto = await _context.Productos.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Slug == slug);
                if (producto == null)
                    return ResultadoOperacion<List<ProductoListadoDto>>.SetExito(new List<ProductoListadoDto>());

                var tipoProducto = producto.Tipo;
                var nowRelacionados = DateTime.UtcNow;
                var productos = await _context.Productos
                    .AsSplitQuery()
                    .AsNoTracking()
                    .Include(p => p.Vendedor)
                    .Include(p => p.Categoria)
                    .Include(p => p.Imagenes)
                    .Include(p => p.Variantes).ThenInclude(v => v.Stock)
                    .Where(p => p.CategoriaId == producto.CategoriaId &&
                                p.Tipo == tipoProducto &&
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
                _logger.LogError(ex, "Error en CatalogoServicio.ObtenerRelacionadosAsync");
                return ResultadoOperacion<List<ProductoListadoDto>>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
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
            var set = await ObtenerCategoriasConDescendientesAsync(new[] { categoriaId });
            return set.ToList();
        }

        private async Task<HashSet<int>> ObtenerCategoriasConDescendientesAsync(IEnumerable<int> categoriaIds)
        {
            const string cacheKey = "todas_categorias_jerarquia_v2";
            var jerarquia = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return await _context.Categorias.AsNoTracking()
                    .Select(c => new CategoriaNodoId
                    {
                        CategoriaId = c.CategoriaId,
                        CategoriaPadreId = c.CategoriaPadreId,
                    })
                    .ToListAsync();
            }) ?? new List<CategoriaNodoId>();

            var resultado = new HashSet<int>();
            foreach (var categoriaId in categoriaIds.Distinct())
            {
                if (!resultado.Add(categoriaId)) continue;
                var pendientes = new Queue<int>();
                pendientes.Enqueue(categoriaId);
                while (pendientes.Count > 0)
                {
                    var actual = pendientes.Dequeue();
                    foreach (var h in jerarquia.Where(c => c.CategoriaPadreId == actual).Select(c => c.CategoriaId))
                    {
                        if (resultado.Add(h))
                            pendientes.Enqueue(h);
                    }
                }
            }
            return resultado;
        }

        private sealed class CategoriaNodoId
        {
            public int CategoriaId { get; set; }
            public int? CategoriaPadreId { get; set; }
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

            // Precio de la variante por defecto (primera creada).
            var variantePorDefecto = p.ObtenerVariantePorDefecto();
            var precioVariantePorDefecto = variantePorDefecto?.Precio ?? p.PrecioBase ?? 0;
            var soloUnaVariante = p.Variantes.Count(v => v.Activa) <= 1;
            var precioCalc = PrecioOfertaUtil.Calcular(
                precioVariantePorDefecto, oferta, soloUnaVariante);

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
                // PrecioBase = precio de la variante por defecto (para tachado si hay oferta).
                PrecioBase = precioVariantePorDefecto,
                TieneVariantes = p.TieneVariantesComprables(),
                VarianteIdDefecto = variantePorDefecto?.VarianteId,
                // PrecioOferta = precio final aplicando el % a la variante por defecto.
                PrecioOferta = precioCalc.TieneDescuento ? precioCalc.PrecioActual : (decimal?)null,
                PorcentajeDescuento = precioCalc.PorcentajeDescuento,
                TieneOferta = precioCalc.TieneDescuento,
                Tipo = new EnumeracionDto { Id = (int)p.Tipo, Nombre = p.Tipo.ToString() },
                CalificacionPromedio = p.CalificacionPromedio,
                TotalResenas = p.TotalResenas,
                TieneStock = tieneStock
            };
        }
    }
}
