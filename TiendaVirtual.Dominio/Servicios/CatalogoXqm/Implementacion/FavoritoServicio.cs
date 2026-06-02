using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Dominio.Servicios.CatalogoXqm.Implementacion
{
    public class FavoritoServicio : IFavoritoServicio
    {
        protected readonly TiendaVirtualDbContext _context;

        public FavoritoServicio(TiendaVirtualDbContext context) => _context = context;

        public async Task<ResultadoOperacion<PaginacionRespuestaDto<FavoritoDto>>> ListarMisFavoritosAsync(
            int usuarioId, int pagina, int tamanioPagina)
        {
            try
            {
                pagina = Math.Max(1, pagina);
                tamanioPagina = Math.Clamp(tamanioPagina, 1, 24);

                var query = _context.Favoritos
                    .AsNoTracking()
                    .Where(f => f.UsuarioId == usuarioId &&
                                f.Producto.Estado == TipoEstadoProducto.Activo);

                var total = await query.CountAsync();
                var favoritos = await query
                    .Include(f => f.Producto).ThenInclude(p => p.Vendedor)
                    .Include(f => f.Producto).ThenInclude(p => p.Categoria)
                    .Include(f => f.Producto).ThenInclude(p => p.Imagenes)
                    .Include(f => f.Producto).ThenInclude(p => p.Variantes).ThenInclude(v => v.Stock)
                    .OrderByDescending(f => f.Fecha)
                    .Skip((pagina - 1) * tamanioPagina)
                    .Take(tamanioPagina)
                    .ToListAsync();

                var productosIds = favoritos.Select(f => f.ProductoId).ToList();
                var now = DateTime.UtcNow;
                var ofertas = await _context.Ofertas.AsNoTracking()
                    .Where(o => productosIds.Contains(o.ProductoId) &&
                                o.Activa && o.FechaInicio <= now && o.FechaFin >= now)
                    .ToListAsync();

                var items = favoritos.Select(f =>
                {
                    var p = f.Producto;
                    var oferta = ofertas.FirstOrDefault(o => o.ProductoId == p.ProductoId);
                    var img = p.Imagenes.FirstOrDefault(i => i.EsPrincipal)?.Url
                              ?? p.Imagenes.OrderBy(i => i.Orden).FirstOrDefault()?.Url;
                    var tieneStock = p.Tipo == TipoProducto.Patron ||
                                     p.Variantes.Any(v => v.Stock != null && v.Stock.CantidadDisponible > 0);

                    return new FavoritoDto
                    {
                        UsuarioId = f.UsuarioId,
                        ProductoId = f.ProductoId,
                        Fecha = f.Fecha,
                        Producto = new ProductoListadoDto
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
                            PrecioBase = p.PrecioBase ?? 0,
                            PrecioOferta = oferta?.PrecioOferta,
                            PorcentajeDescuento = oferta?.PorcentajeDescuento,
                            TieneOferta = oferta != null,
                            Tipo = new EnumeracionDto { Id = (int)p.Tipo, Nombre = p.Tipo.ToString() },
                            CalificacionPromedio = p.CalificacionPromedio,
                            TotalResenas = p.TotalResenas,
                            TieneStock = tieneStock
                        }
                    };
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
                return ResultadoOperacion<PaginacionRespuestaDto<FavoritoDto>>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> AgregarAsync(int usuarioId, int productoId)
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
                return ResultadoOperacion<bool>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> QuitarAsync(int usuarioId, int productoId)
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
                return ResultadoOperacion<bool>.SetError("Error: " + ex.Message);
            }
        }
    }
}
