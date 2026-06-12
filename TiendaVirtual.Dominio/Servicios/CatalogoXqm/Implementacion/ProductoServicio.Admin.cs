using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Utilidad;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Dominio.Servicios.CatalogoXqm.Implementacion
{
    public partial class ProductoServicio
    {
        public async Task<ResultadoOperacion<PaginacionRespuestaDto<ProductoAdminListadoDto>>> ListarAdminAsync(
            string? busqueda, int? vendedorId, int? categoriaId, string? estado,
            int pagina, int tamanioPagina)
        {
            try
            {
                pagina = Math.Max(1, pagina);
                tamanioPagina = Math.Clamp(tamanioPagina, 1, 50);

                var query = _context.Productos.AsNoTracking()
                    .Include(p => p.Vendedor)
                    .Include(p => p.Categoria)
                    .Include(p => p.Imagenes)
                    .Include(p => p.Variantes).ThenInclude(v => v.Stock)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    var term = busqueda.Trim().ToLower();
                    query = query.Where(p =>
                        p.Nombre.ToLower().Contains(term) ||
                        p.Vendedor.NombreTienda.ToLower().Contains(term));
                }
                if (vendedorId.HasValue) query = query.Where(p => p.VendedorId == vendedorId.Value);
                if (categoriaId.HasValue) query = query.Where(p => p.CategoriaId == categoriaId.Value);

                if (!string.IsNullOrWhiteSpace(estado) && estado != "todos")
                {
                    query = estado.ToLower() switch
                    {
                        "activo" => query.Where(p => p.Estado == TipoEstadoProducto.Activo),
                        "pausado" => query.Where(p => p.Estado == TipoEstadoProducto.Pausado &&
                            string.IsNullOrEmpty(p.MotivoPausaAdmin)),
                        "borrador" => query.Where(p => p.Estado == TipoEstadoProducto.Borrador),
                        "ocultoadmin" => query.Where(p => !string.IsNullOrEmpty(p.MotivoPausaAdmin)),
                        _ => query
                    };
                }

                var total = await query.CountAsync();
                var productos = await query.OrderByDescending(p => p.ProductoId)
                    .Skip((pagina - 1) * tamanioPagina).Take(tamanioPagina).ToListAsync();

                var items = productos.Select(p => new ProductoAdminListadoDto
                {
                    ProductoId = p.ProductoId,
                    Nombre = p.Nombre,
                    Slug = p.Slug,
                    NombreTienda = p.Vendedor.NombreTienda,
                    VendedorId = p.VendedorId,
                    NombreCategoria = p.Categoria.Nombre,
                    PrecioBase = p.PrecioBase,
                    StockDisponible = p.Variantes.Where(v => v.Activa && v.Stock != null)
                        .Sum(v => v.Stock!.CantidadDisponible),
                    ImagenPrincipalUrl = p.Imagenes.OrderByDescending(i => i.EsPrincipal)
                        .ThenBy(i => i.Orden).FirstOrDefault()?.Url,
                    Estado = new EnumeracionDto((int)p.Estado, p.Estado.GetDescription()),
                    MotivoPausaAdmin = p.MotivoPausaAdmin
                }).ToList();

                return ResultadoOperacion<PaginacionRespuestaDto<ProductoAdminListadoDto>>.SetExito(
                    new PaginacionRespuestaDto<ProductoAdminListadoDto>
                    {
                        Items = items, Pagina = pagina, TamanioPagina = tamanioPagina,
                        TotalRegistros = total, HayMas = pagina * tamanioPagina < total
                    });
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<PaginacionRespuestaDto<ProductoAdminListadoDto>>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> OcultarAdminAsync(int productoId, OcultarProductoDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Motivo))
                    return ResultadoOperacion<bool>.SetError("El motivo es obligatorio.");

                var p = await _context.Productos.Include(x => x.Vendedor)
                    .FirstOrDefaultAsync(x => x.ProductoId == productoId);
                if (p == null) return ResultadoOperacion<bool>.SetError("Producto no encontrado.");

                p.Estado = TipoEstadoProducto.Pausado;
                p.MotivoPausaAdmin = dto.Motivo.Trim();
                await _context.SaveChangesAsync();

                await _notificacionServicio.CrearAsync(
                    p.Vendedor.UsuarioId,
                    TipoNotificacion.ProductoOcultoAdmin,
                    "Producto oculto por administración",
                    $"Tu producto \"{p.Nombre}\" fue ocultado. Motivo: {dto.Motivo.Trim()}");

                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> RestaurarAdminAsync(int productoId)
        {
            try
            {
                var p = await _context.Productos.FindAsync(productoId);
                if (p == null) return ResultadoOperacion<bool>.SetError("Producto no encontrado.");

                p.MotivoPausaAdmin = null;
                p.Estado = TipoEstadoProducto.Activo;
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
