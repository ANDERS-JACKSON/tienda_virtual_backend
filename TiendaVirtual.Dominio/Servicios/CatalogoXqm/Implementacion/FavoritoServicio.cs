using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Extensiones.CatalogoXqm;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;
using TiendaVirtual.Dominio.Utilidad;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Dominio.Servicios.CatalogoXqm.Implementacion
{
    public class FavoritoServicio : IFavoritoServicio
    {
        protected readonly TiendaVirtualDbContext _context;
        private readonly ILogger<FavoritoServicio> _logger;

        public FavoritoServicio(TiendaVirtualDbContext context, ILogger<FavoritoServicio> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ResultadoOperacion<PaginacionRespuestaDto<FavoritoDto>>> ListarMisFavoritosAsync(
            Guid usuarioId, int pagina, int tamanioPagina)
        {
            try
            {
                pagina = Math.Max(1, pagina);
                tamanioPagina = Math.Clamp(tamanioPagina, 1, 24);

                var now = DateTime.UtcNow;
                var query = _context.Favoritos
                    .AsNoTracking()
                    .Where(f => f.UsuarioId == usuarioId &&
                                f.Producto.Estado == TipoEstadoProducto.Activo)
                    .Where(f => _context.Suscripciones.Any(s =>
                        s.VendedorId == f.Producto.VendedorId &&
                        ((s.Estado == TipoEstadoSuscripcion.EnPrueba &&
                          s.PruebaTerminaEn.HasValue &&
                          s.PruebaTerminaEn > now) ||
                         (s.Estado == TipoEstadoSuscripcion.Activa &&
                          (!s.PeriodoFin.HasValue || s.PeriodoFin > now)))));

                var total = await query.CountAsync();
                var favoritos = await query
                    .AsSplitQuery()
                    .Include(f => f.Producto).ThenInclude(p => p.Vendedor)
                    .Include(f => f.Producto).ThenInclude(p => p.Categoria)
                    .Include(f => f.Producto).ThenInclude(p => p.Imagenes)
                    .Include(f => f.Producto).ThenInclude(p => p.Variantes).ThenInclude(v => v.Stock)
                    .OrderByDescending(f => f.Fecha)
                    .Skip((pagina - 1) * tamanioPagina)
                    .Take(tamanioPagina)
                    .ToListAsync();

                var productosIds = favoritos.Select(f => f.ProductoId).ToList();
                var ofertasVigentes = await ObtenerOfertasVigentesAsync(productosIds, now);

                var items = favoritos.Select(f => new FavoritoDto
                {
                    UsuarioId = f.UsuarioId,
                    ProductoId = f.ProductoId,
                    Fecha = f.Fecha,
                    Producto = MapearProductoListado(f.Producto, ofertasVigentes)
                }).ToList();

                return ResultadoOperacion<PaginacionRespuestaDto<FavoritoDto>>.SetExito(
                    new PaginacionRespuestaDto<FavoritoDto>
                    {
                        Items = items,
                        Pagina = pagina,
                        TamanioPagina = tamanioPagina,
                        TotalRegistros = total,
                        HayMas = pagina * tamanioPagina < total
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en FavoritoServicio.");
                return ResultadoOperacion<PaginacionRespuestaDto<FavoritoDto>>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        public async Task<ResultadoOperacion<bool>> AgregarAsync(Guid usuarioId, int productoId)
        {
            try
            {
                if (!await _context.Productos.AnyAsync(p =>
                        p.ProductoId == productoId && p.Estado == TipoEstadoProducto.Activo))
                    return ResultadoOperacion<bool>.SetError("Producto no encontrado.");

                var existe = await _context.Favoritos.AnyAsync(f =>
                    f.UsuarioId == usuarioId && f.ProductoId == productoId);
                if (existe) return ResultadoOperacion<bool>.SetExito(true); // idempotente

                _context.Favoritos.Add(new Favorito
                {
                    UsuarioId = usuarioId,
                    ProductoId = productoId,
                    Fecha = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en FavoritoServicio.");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        public async Task<ResultadoOperacion<bool>> QuitarAsync(Guid usuarioId, int productoId)
        {
            try
            {
                var fav = await _context.Favoritos
                    .FirstOrDefaultAsync(f => f.UsuarioId == usuarioId && f.ProductoId == productoId);
                if (fav == null) return ResultadoOperacion<bool>.SetExito(true); // idempotente

                _context.Favoritos.Remove(fav);
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en FavoritoServicio.QuitarAsync");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        /// <summary>Misma regla que CatalogoServicio: precio de variante + oferta vigente.</summary>
        private static ProductoListadoDto MapearProductoListado(
            Producto p, Dictionary<int, Oferta> ofertas)
        {
            var img = p.Imagenes.FirstOrDefault(i => i.EsPrincipal)?.Url
                      ?? p.Imagenes.OrderBy(i => i.Orden).FirstOrDefault()?.Url;

            ofertas.TryGetValue(p.ProductoId, out var oferta);
            var tieneStock = p.Tipo == TipoProducto.Patron ||
                             p.Variantes.Any(v => v.Stock != null && v.Stock.CantidadDisponible > 0);

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
                ImagenPrincipalUrl = img,
                VendedorId = p.VendedorId,
                NombreTienda = p.Vendedor.NombreTienda,
                SlugTienda = p.Vendedor.SlugTienda,
                CategoriaId = p.CategoriaId,
                NombreCategoria = p.Categoria.Nombre,
                PrecioBase = precioVariantePorDefecto,
                TieneVariantes = p.TieneVariantesComprables(),
                VarianteIdDefecto = variantePorDefecto?.VarianteId,
                PrecioOferta = precioCalc.TieneDescuento ? precioCalc.PrecioActual : null,
                PorcentajeDescuento = precioCalc.PorcentajeDescuento,
                TieneOferta = precioCalc.TieneDescuento,
                Tipo = new EnumeracionDto { Id = (int)p.Tipo, Nombre = p.Tipo.ToString() },
                CalificacionPromedio = p.CalificacionPromedio,
                TotalResenas = p.TotalResenas,
                TieneStock = tieneStock,
                FechaPublicacion = p.FechaPublicacion
            };
        }

        private async Task<Dictionary<int, Oferta>> ObtenerOfertasVigentesAsync(
            List<int> productosIds, DateTime now)
        {
            if (productosIds.Count == 0) return new Dictionary<int, Oferta>();

            var ofertas = await _context.Ofertas.AsNoTracking()
                .Where(o => productosIds.Contains(o.ProductoId) &&
                            o.Activa && o.FechaInicio <= now && o.FechaFin >= now)
                .OrderByDescending(o => o.OfertaId)
                .ToListAsync();

            return ofertas
                .GroupBy(o => o.ProductoId)
                .ToDictionary(g => g.Key, g => g.First());
        }
    }
}
