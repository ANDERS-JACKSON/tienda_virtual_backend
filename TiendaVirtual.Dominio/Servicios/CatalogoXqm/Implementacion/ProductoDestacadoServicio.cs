using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;
using TiendaVirtual.Dominio.Utilidad;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Dominio.Servicios.CatalogoXqm.Implementacion
{
    public class ProductoDestacadoServicio : IProductoDestacadoServicio
    {
        private const int MAX_DESTACADOS = 10;
        private const string CacheKeyPublico = "destacados:publico";

        private readonly TiendaVirtualDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ProductoDestacadoServicio> _logger;

        public ProductoDestacadoServicio(
            TiendaVirtualDbContext context,
            IMemoryCache cache,
            ILogger<ProductoDestacadoServicio> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ResultadoOperacion<List<ProductoDestacadoPublicoDto>>> ListarPublicoAsync()
        {
            try
            {
                if (_cache.TryGetValue(CacheKeyPublico, out List<ProductoDestacadoPublicoDto>? cached) &&
                    cached != null)
                {
                    return ResultadoOperacion<List<ProductoDestacadoPublicoDto>>.SetExito(cached);
                }

                var now = DateTime.UtcNow;
                var destacados = await _context.ProductosDestacados
                    .AsNoTracking()
                    .Include(d => d.Producto).ThenInclude(p => p.Vendedor)
                    .Include(d => d.Producto).ThenInclude(p => p.Imagenes)
                    .Include(d => d.Producto).ThenInclude(p => p.Ofertas)
                    .Include(d => d.Producto).ThenInclude(p => p.Variantes).ThenInclude(v => v.Stock)
                    .OrderBy(d => d.Orden)
                    .ThenBy(d => d.ProductoDestacadoId)
                    .Take(MAX_DESTACADOS)
                    .ToListAsync();

                var items = destacados
                    .Where(d =>
                        d.Producto.Estado == TipoEstadoProducto.Activo &&
                        string.IsNullOrEmpty(d.Producto.MotivoPausaAdmin))
                    .Select(MapearPublico)
                    .ToList();

                _cache.Set(CacheKeyPublico, items, TimeSpan.FromMinutes(5));
                return ResultadoOperacion<List<ProductoDestacadoPublicoDto>>.SetExito(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listando productos destacados públicos");
                return ResultadoOperacion<List<ProductoDestacadoPublicoDto>>.SetError(
                    "Ocurrió un error al obtener los productos destacados.");
            }
        }

        public async Task<ResultadoOperacion<List<ProductoDestacadoAdminDto>>> ListarAdminAsync()
        {
            try
            {
                var destacados = await _context.ProductosDestacados
                    .AsNoTracking()
                    .Include(d => d.Producto).ThenInclude(p => p.Vendedor)
                    .Include(d => d.Producto).ThenInclude(p => p.Imagenes)
                    .OrderBy(d => d.Orden)
                    .ThenBy(d => d.ProductoDestacadoId)
                    .ToListAsync();

                var items = destacados.Select(MapearAdmin).ToList();
                return ResultadoOperacion<List<ProductoDestacadoAdminDto>>.SetExito(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listando productos destacados admin");
                return ResultadoOperacion<List<ProductoDestacadoAdminDto>>.SetError(
                    "Ocurrió un error al obtener los productos destacados.");
            }
        }

        public async Task<ResultadoOperacion<ProductoDestacadoAdminDto>> AgregarAsync(
            AgregarProductoDestacadoDto dto)
        {
            try
            {
                if (dto == null || dto.ProductoId <= 0)
                    return ResultadoOperacion<ProductoDestacadoAdminDto>.SetError("Producto no válido.");

                var productoExiste = await _context.Productos
                    .AsNoTracking()
                    .AnyAsync(p => p.ProductoId == dto.ProductoId);
                if (!productoExiste)
                    return ResultadoOperacion<ProductoDestacadoAdminDto>.SetError("Producto no encontrado.");

                var yaDestacado = await _context.ProductosDestacados
                    .AsNoTracking()
                    .AnyAsync(d => d.ProductoId == dto.ProductoId);
                if (yaDestacado)
                    return ResultadoOperacion<ProductoDestacadoAdminDto>.SetError(
                        "Este producto ya está en destacados.");

                var total = await _context.ProductosDestacados.CountAsync();
                if (total >= MAX_DESTACADOS)
                {
                    return ResultadoOperacion<ProductoDestacadoAdminDto>.SetError(
                        $"Solo se permiten {MAX_DESTACADOS} productos destacados. Elimina alguno primero.");
                }

                var maxOrden = await _context.ProductosDestacados
                    .MaxAsync(d => (int?)d.Orden) ?? 0;

                var entidad = new ProductoDestacado
                {
                    ProductoId = dto.ProductoId,
                    Orden = maxOrden + 10
                };

                _context.ProductosDestacados.Add(entidad);
                await _context.SaveChangesAsync();
                InvalidarCachePublico();

                var creado = await _context.ProductosDestacados
                    .AsNoTracking()
                    .Include(d => d.Producto).ThenInclude(p => p.Vendedor)
                    .Include(d => d.Producto).ThenInclude(p => p.Imagenes)
                    .FirstAsync(d => d.ProductoDestacadoId == entidad.ProductoDestacadoId);

                return ResultadoOperacion<ProductoDestacadoAdminDto>.SetExito(MapearAdmin(creado));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error agregando producto destacado {ProductoId}", dto?.ProductoId);
                return ResultadoOperacion<ProductoDestacadoAdminDto>.SetError(
                    "Ocurrió un error al agregar el producto destacado.");
            }
        }

        public async Task<ResultadoOperacion<bool>> EliminarAsync(int destacadoId)
        {
            try
            {
                var entidad = await _context.ProductosDestacados
                    .FirstOrDefaultAsync(d => d.ProductoDestacadoId == destacadoId);
                if (entidad == null)
                    return ResultadoOperacion<bool>.SetError("No encontrado.");

                _context.ProductosDestacados.Remove(entidad);
                await _context.SaveChangesAsync();
                InvalidarCachePublico();

                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando producto destacado {DestacadoId}", destacadoId);
                return ResultadoOperacion<bool>.SetError(
                    "Ocurrió un error al eliminar el producto destacado.");
            }
        }

        public async Task<ResultadoOperacion<bool>> ReordenarAsync(ReordenarProductosDestacadosDto dto)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                if (dto?.Items == null || dto.Items.Count == 0)
                    return ResultadoOperacion<bool>.SetError("La lista de reordenamiento está vacía.");

                var existentes = await _context.ProductosDestacados.ToListAsync();
                var idsValidos = existentes.Select(d => d.ProductoDestacadoId).ToHashSet();

                foreach (var item in dto.Items)
                {
                    if (!idsValidos.Contains(item.ProductoDestacadoId))
                        return ResultadoOperacion<bool>.SetError("Algún id no existe.");
                }

                var mapaOrden = dto.Items.ToDictionary(i => i.ProductoDestacadoId, i => i.Orden);
                foreach (var entidad in existentes)
                {
                    if (mapaOrden.TryGetValue(entidad.ProductoDestacadoId, out var nuevoOrden))
                        entidad.Orden = nuevoOrden;
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                InvalidarCachePublico();

                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error reordenando productos destacados");
                return ResultadoOperacion<bool>.SetError(
                    "Ocurrió un error al reordenar los productos destacados.");
            }
        }

        private void InvalidarCachePublico() => _cache.Remove(CacheKeyPublico);

        private static ProductoDestacadoPublicoDto MapearPublico(ProductoDestacado d)
        {
            var p = d.Producto;
            var now = DateTime.UtcNow;
            var oferta = p.Ofertas
                .Where(o => o.Activa && o.FechaInicio <= now && o.FechaFin >= now)
                .OrderByDescending(o => o.OfertaId)
                .FirstOrDefault();

            return new ProductoDestacadoPublicoDto
            {
                ProductoDestacadoId = d.ProductoDestacadoId,
                ProductoId = p.ProductoId,
                Nombre = p.Nombre,
                Slug = p.Slug,
                NombreTienda = p.Vendedor?.NombreTienda,
                PrecioBase = p.PrecioBase,
                PrecioOferta = oferta?.PrecioOferta,
                PorcentajeDescuento = oferta?.PorcentajeDescuento,
                ImagenPrincipalPublicId = ObtenerImagenPrincipal(p),
                Orden = d.Orden,
                Tipo = new EnumeracionDto((int)p.Tipo, p.Tipo.ToString()),
                CalificacionPromedio = p.CalificacionPromedio,
                TotalResenas = p.TotalResenas,
                TieneStock = p.Tipo == TipoProducto.Patron ||
                             p.Variantes.Any(v => v.Stock != null && v.Stock.CantidadDisponible > 0)
            };
        }

        private static ProductoDestacadoAdminDto MapearAdmin(ProductoDestacado d)
        {
            var p = d.Producto;
            return new ProductoDestacadoAdminDto
            {
                ProductoDestacadoId = d.ProductoDestacadoId,
                ProductoId = p.ProductoId,
                Nombre = p.Nombre,
                NombreTienda = p.Vendedor?.NombreTienda,
                ImagenPrincipalPublicId = ObtenerImagenPrincipal(p),
                Orden = d.Orden,
                EstadoProducto = new EnumeracionDto((int)p.Estado, p.Estado.GetDescription()),
                OcultoPorAdmin = !string.IsNullOrEmpty(p.MotivoPausaAdmin)
            };
        }

        private static string? ObtenerImagenPrincipal(Producto p)
        {
            return p.Imagenes.FirstOrDefault(i => i.EsPrincipal)?.Url
                   ?? p.Imagenes.OrderBy(i => i.Orden).FirstOrDefault()?.Url;
        }
    }
}
