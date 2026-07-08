using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.VentaXqm;
using TiendaVirtual.Dominio.Utilidad;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Servicios.VentaXqm.Implementacion
{
    /// <summary>
    /// Maneja el carrito del usuario (1 por usuario). Internamente garantiza
    /// que exista uno antes de cualquier mutación.
    /// </summary>
    public class CarritoServicio : ICarritoServicio
    {
        // PATRON: producto digital, sin stock físico. Convención: 999999 disponibles.
        private const int STOCK_INFINITO_PATRON = 999999;

        protected readonly TiendaVirtualDbContext _context;
        private readonly ILogger<CarritoServicio> _logger;

        public CarritoServicio(TiendaVirtualDbContext context, ILogger<CarritoServicio> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ResultadoOperacion<CarritoDto>> ObtenerMiCarritoAsync(int usuarioId)
        {
            try
            {
                var carrito = await ObtenerOCrearCarritoAsync(usuarioId);
                return ResultadoOperacion<CarritoDto>.SetExito(
                    await ConstruirCarritoDtoAsync(carrito.CarritoId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CarritoServicio.ObtenerMiCarritoAsync");
                return ResultadoOperacion<CarritoDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<CarritoDto>> AgregarItemAsync(int usuarioId, AgregarItemCarritoDto dto)
        {
            try
            {
                if (dto.Cantidad <= 0)
                    return ResultadoOperacion<CarritoDto>.SetError("La cantidad debe ser mayor a 0.");

                var variante = await _context.VariantesProducto
                    .Include(v => v.Stock)
                    .Include(v => v.Producto).ThenInclude(p => p.Vendedor)
                    .FirstOrDefaultAsync(v => v.VarianteId == dto.VarianteId && v.Activa);

                if (variante == null)
                    return ResultadoOperacion<CarritoDto>.SetError("La variante no existe o está inactiva.");

                if (variante.Producto.Estado != TipoEstadoProducto.Activo)
                    return ResultadoOperacion<CarritoDto>.SetError("Este producto no está disponible.");

                var now = DateTime.UtcNow;
                var tiendaActiva = await _context.Suscripciones.AnyAsync(s =>
                    s.VendedorId == variante.Producto.VendedorId &&
                    ((s.Estado == TipoEstadoSuscripcion.EnPrueba &&
                      s.PruebaTerminaEn.HasValue &&
                      s.PruebaTerminaEn > now) ||
                     (s.Estado == TipoEstadoSuscripcion.Activa &&
                      (!s.PeriodoFin.HasValue || s.PeriodoFin > now))));
                if (!tiendaActiva)
                    return ResultadoOperacion<CarritoDto>.SetError(
                        "La tienda de este producto no está disponible en este momento.");

                // El vendedor no puede comprar sus propios productos.
                var esPropio = await _context.Vendedores.AnyAsync(v =>
                    v.UsuarioId == usuarioId && v.VendedorId == variante.Producto.VendedorId);
                if (esPropio)
                    return ResultadoOperacion<CarritoDto>.SetError("No puedes comprar tus propios productos.");

                var carrito = await ObtenerOCrearCarritoAsync(usuarioId);

                var existente = await _context.ItemsCarrito.FirstOrDefaultAsync(i =>
                    i.CarritoId == carrito.CarritoId && i.VarianteId == dto.VarianteId);

                var cantidadFinal = (existente?.Cantidad ?? 0) + dto.Cantidad;
                var stockDisponible = ObtenerStockDisponible(variante);

                if (cantidadFinal > stockDisponible)
                    return ResultadoOperacion<CarritoDto>.SetError(
                        "No hay stock suficiente para la cantidad que pediste.");

                if (existente != null)
                {
                    existente.Cantidad = cantidadFinal;
                }
                else
                {
                    _context.ItemsCarrito.Add(new ItemCarrito
                    {
                        CarritoId = carrito.CarritoId,
                        VarianteId = dto.VarianteId,
                        Cantidad = dto.Cantidad,
                        FechaAgregado = DateTime.UtcNow
                    });
                }

                carrito.FechaActualizacion = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return ResultadoOperacion<CarritoDto>.SetExito(
                    await ConstruirCarritoDtoAsync(carrito.CarritoId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CarritoServicio.AgregarItemAsync");
                return ResultadoOperacion<CarritoDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<CarritoDto>> ActualizarItemAsync(
            int usuarioId, int itemId, ActualizarItemCarritoDto dto)
        {
            try
            {
                if (dto.Cantidad <= 0)
                    return ResultadoOperacion<CarritoDto>.SetError("La cantidad debe ser mayor a 0.");

                var carrito = await ObtenerOCrearCarritoAsync(usuarioId);

                var item = await _context.ItemsCarrito
                    .Include(i => i.Variante).ThenInclude(v => v.Stock)
                    .Include(i => i.Variante).ThenInclude(v => v.Producto)
                    .FirstOrDefaultAsync(i => i.ItemCarritoId == itemId && i.CarritoId == carrito.CarritoId);

                if (item == null)
                    return ResultadoOperacion<CarritoDto>.SetError("Item no encontrado en tu carrito.");

                var stockDisponible = ObtenerStockDisponible(item.Variante);
                if (dto.Cantidad > stockDisponible)
                    return ResultadoOperacion<CarritoDto>.SetError(
                        "No hay stock suficiente para la cantidad que pediste.");

                item.Cantidad = dto.Cantidad;
                carrito.FechaActualizacion = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return ResultadoOperacion<CarritoDto>.SetExito(
                    await ConstruirCarritoDtoAsync(carrito.CarritoId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CarritoServicio.ActualizarItemAsync");
                return ResultadoOperacion<CarritoDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<CarritoDto>> QuitarItemAsync(int usuarioId, int itemId)
        {
            try
            {
                var carrito = await ObtenerOCrearCarritoAsync(usuarioId);

                var item = await _context.ItemsCarrito.FirstOrDefaultAsync(i =>
                    i.ItemCarritoId == itemId && i.CarritoId == carrito.CarritoId);
                if (item == null)
                    return ResultadoOperacion<CarritoDto>.SetError("Item no encontrado en tu carrito.");

                _context.ItemsCarrito.Remove(item);
                carrito.FechaActualizacion = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return ResultadoOperacion<CarritoDto>.SetExito(
                    await ConstruirCarritoDtoAsync(carrito.CarritoId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CarritoServicio.QuitarItemAsync");
                return ResultadoOperacion<CarritoDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<bool>> VaciarAsync(int usuarioId)
        {
            try
            {
                var carrito = await ObtenerOCrearCarritoAsync(usuarioId);

                var items = await _context.ItemsCarrito
                    .Where(i => i.CarritoId == carrito.CarritoId)
                    .ToListAsync();
                _context.ItemsCarrito.RemoveRange(items);
                carrito.FechaActualizacion = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CarritoServicio.VaciarAsync");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        // ─────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────
        private async Task<Carrito> ObtenerOCrearCarritoAsync(int usuarioId)
        {
            var carrito = await _context.Carritos.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
            if (carrito == null)
            {
                carrito = new Carrito
                {
                    UsuarioId = usuarioId,
                    FechaActualizacion = DateTime.UtcNow
                };
                _context.Carritos.Add(carrito);
                await _context.SaveChangesAsync();
            }
            return carrito;
        }

        private static int ObtenerStockDisponible(Modelo.CatalogoXqm.VarianteProducto variante)
        {
            if (variante.Producto.Tipo == TipoProducto.Patron)
                return STOCK_INFINITO_PATRON;
            return variante.Stock?.CantidadDisponible ?? 0;
        }

        private async Task<CarritoDto> ConstruirCarritoDtoAsync(int carritoId)
        {
            // Cargamos items sin traer Producto.Variantes en el mismo grafo para evitar
            // el ciclo Variante -> Producto -> Variantes bajo AsNoTracking.
            var items = await _context.ItemsCarrito
                .AsSplitQuery()
                .AsNoTracking()
                .Include(i => i.Variante).ThenInclude(v => v.Stock)
                .Include(i => i.Variante).ThenInclude(v => v.Producto).ThenInclude(p => p.Vendedor)
                .Include(i => i.Variante).ThenInclude(v => v.Producto).ThenInclude(p => p.Imagenes)
                .Where(i => i.CarritoId == carritoId)
                .OrderByDescending(i => i.FechaAgregado)
                .ToListAsync();

            if (items.Count == 0)
                return new CarritoDto();

            var productosIds = items.Select(i => i.Variante.ProductoId).Distinct().ToList();
            var now = DateTime.UtcNow;

            // Cargamos ofertas vigentes de TODOS los productos del carrito en una sola query.
            // Nota: siempre se consulta al momento; si la oferta cambia o termina, el subtotal
            // se recalcula la próxima vez que el cliente consulte el carrito.
            var ofertas = await _context.Ofertas.AsNoTracking()
                .Where(o => productosIds.Contains(o.ProductoId)
                            && o.Activa
                            && o.FechaInicio <= now
                            && o.FechaFin >= now)
                .OrderByDescending(o => o.OfertaId)
                .ToListAsync();

            // Conteo de variantes activas por producto (para decidir si aceptar precio fijo
            // de la oferta como fallback en productos de una sola variante).
            var conteoVariantesActivas = await _context.VariantesProducto.AsNoTracking()
                .Where(v => productosIds.Contains(v.ProductoId) && v.Activa)
                .GroupBy(v => v.ProductoId)
                .Select(g => new { ProductoId = g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.ProductoId, x => x.Total);

            var itemsDto = items.Select(i =>
            {
                var producto = i.Variante.Producto;
                var oferta = ofertas.FirstOrDefault(o => o.ProductoId == producto.ProductoId);

                // Aplica el porcentaje de la oferta al precio ACTUAL de la variante en carrito.
                var totalActivas = conteoVariantesActivas.TryGetValue(producto.ProductoId, out var c) ? c : 0;
                var soloUnaVariante = totalActivas <= 1;
                var precioCalc = PrecioOfertaUtil.Calcular(
                    i.Variante.Precio, oferta, soloUnaVariante);

                var imagen = producto.Imagenes.FirstOrDefault(im => im.EsPrincipal)?.Url
                             ?? producto.Imagenes.OrderBy(im => im.Orden).FirstOrDefault()?.Url;

                // Stock real solo se usa dentro del servidor para decidir los
                // flags que sí se serializan. NUNCA se expone al comprador.
                var stockDisponible = producto.Tipo == TipoProducto.Patron
                    ? STOCK_INFINITO_PATRON
                    : (i.Variante.Stock?.CantidadDisponible ?? 0);

                var stockSuficiente = i.Cantidad <= stockDisponible;

                // Cuando el stock alcanza para la cantidad pedida, damos un techo
                // razonable en el stepper para no revelar el stock total. Si NO
                // alcanza, mostramos el máximo real (que ya conoce por la falla).
                var cantidadMaximaPermitida = stockSuficiente
                    ? Math.Min(i.Cantidad + 10, stockDisponible)
                    : stockDisponible;

                return new ItemCarritoDto
                {
                    ItemCarritoId = i.ItemCarritoId,
                    VarianteId = i.VarianteId,
                    ProductoId = producto.ProductoId,
                    Slug = producto.Slug,
                    NombreProducto = producto.Nombre,
                    NombreVariante = i.Variante.Nombre,
                    ImagenUrl = imagen,
                    PrecioUnitario = precioCalc.PrecioActual,
                    PrecioOriginal = precioCalc.TieneDescuento ? precioCalc.PrecioOriginal : null,
                    Cantidad = i.Cantidad,
                    StockSuficiente = stockSuficiente,
                    CantidadMaximaPermitida = cantidadMaximaPermitida,
                    Subtotal = Math.Round(precioCalc.PrecioActual * i.Cantidad, 2),
                    TipoProducto = new EnumeracionDto
                    {
                        Id = (int)producto.Tipo,
                        Nombre = producto.Tipo.ToString()
                    },
                    VendedorIdInterno = producto.VendedorId,
                    NombreTiendaInterno = producto.Vendedor.NombreTienda,
                    SlugTiendaInterno = producto.Vendedor.SlugTienda
                };
            }).ToList();

            var grupos = itemsDto
                .GroupBy(i => i.VendedorIdInterno)
                .Select(g => new GrupoVendedorCarritoDto
                {
                    VendedorId = g.Key,
                    NombreTienda = g.First().NombreTiendaInterno,
                    SlugTienda = g.First().SlugTiendaInterno,
                    Items = g.ToList(),
                    SubtotalGrupo = Math.Round(g.Sum(i => i.Subtotal), 2)
                })
                .OrderBy(g => g.NombreTienda)
                .ToList();

            return new CarritoDto
            {
                Vendedores = grupos,
                TotalItems = itemsDto.Sum(i => i.Cantidad),
                Subtotal = Math.Round(itemsDto.Sum(i => i.Subtotal), 2),
                TieneItemsSinStock = itemsDto.Any(i => !i.StockSuficiente)
            };
        }
    }
}
