using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.SoporteXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.SoporteXqm;

namespace TiendaVirtual.Dominio.Servicios.SoporteXqm.Implementacion
{
    public class ResenaServicio : IResenaServicio
    {
        private readonly TiendaVirtualDbContext _context;
        private readonly INotificacionServicio _notif;

        public ResenaServicio(TiendaVirtualDbContext context, INotificacionServicio notif)
        {
            _context = context;
            _notif = notif;
        }

        public async Task<ResultadoOperacion<ResenaProductoDto>> CrearResenaProductoAsync(
            int usuarioId, CrearResenaProductoDto dto)
        {
            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                var item = await _context.ItemsOrden
                    .Include(i => i.Suborden).ThenInclude(s => s.Orden)
                    .Include(i => i.Suborden).ThenInclude(s => s.Vendedor)
                    .FirstOrDefaultAsync(i => i.ItemOrdenId == dto.ItemOrdenId);

                if (item == null)
                    return ResultadoOperacion<ResenaProductoDto>.SetError("Item de orden no encontrado.");

                if (item.Suborden.Orden.ClienteId != usuarioId)
                    return ResultadoOperacion<ResenaProductoDto>.SetError("Este pedido no es tuyo.");

                if (item.Suborden.Estado != TipoEstadoSuborden.Entregada)
                    return ResultadoOperacion<ResenaProductoDto>.SetError(
                        "Solo puedes reseñar productos de pedidos entregados.");

                if (await _context.ResenasProducto.AnyAsync(r => r.ItemOrdenId == dto.ItemOrdenId))
                    return ResultadoOperacion<ResenaProductoDto>.SetError("Ya reseñaste este producto.");

                int? productoId = null;
                if (item.VarianteId.HasValue)
                {
                    productoId = await _context.VariantesProducto
                        .Where(v => v.VarianteId == item.VarianteId)
                        .Select(v => (int?)v.ProductoId)
                        .FirstOrDefaultAsync();
                }

                if (productoId == null)
                    return ResultadoOperacion<ResenaProductoDto>.SetError(
                        "El producto original ya no existe. No puede ser reseñado.");

                var resena = new ResenaProducto
                {
                    ProductoId = productoId.Value,
                    ItemOrdenId = dto.ItemOrdenId,
                    ClienteId = usuarioId,
                    Calificacion = (short)dto.Calificacion,
                    Titulo = dto.Titulo?.Trim(),
                    Comentario = dto.Comentario?.Trim(),
                    Imagenes = dto.Imagenes.Count > 0 ? JsonSerializer.Serialize(dto.Imagenes) : null,
                    Fecha = DateTime.UtcNow
                };
                _context.ResenasProducto.Add(resena);
                await _context.SaveChangesAsync();

                await RecalcularPromedioProductoAsync(productoId.Value);
                await trx.CommitAsync();

                await _notif.CrearAsync(
                    item.Suborden.Vendedor.UsuarioId,
                    TipoNotificacion.ResenaRecibida,
                    $"Recibiste una reseña de {dto.Calificacion}/5 estrellas",
                    $"Producto: {item.NombreProducto}. " + (dto.Comentario ?? string.Empty),
                    new { resenaId = resena.ResenaId, productoId });

                return await ObtenerResenaProductoDtoAsync(resena.ResenaId);
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                return ResultadoOperacion<ResenaProductoDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<ResenaVendedorDto>> CrearResenaVendedorAsync(
            int usuarioId, CrearResenaVendedorDto dto)
        {
            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                var suborden = await _context.Subordenes
                    .Include(s => s.Orden)
                    .Include(s => s.Vendedor)
                    .FirstOrDefaultAsync(s => s.SubordenId == dto.SubordenId);

                if (suborden == null)
                    return ResultadoOperacion<ResenaVendedorDto>.SetError("Suborden no encontrada.");

                if (suborden.Orden.ClienteId != usuarioId)
                    return ResultadoOperacion<ResenaVendedorDto>.SetError("Este pedido no es tuyo.");

                if (suborden.Estado != TipoEstadoSuborden.Entregada)
                    return ResultadoOperacion<ResenaVendedorDto>.SetError(
                        "Solo puedes reseñar tras la entrega del pedido.");

                if (await _context.ResenasVendedor.AnyAsync(r => r.SubordenId == dto.SubordenId))
                    return ResultadoOperacion<ResenaVendedorDto>.SetError(
                        "Ya reseñaste a este vendedor por este pedido.");

                var resena = new ResenaVendedor
                {
                    VendedorId = suborden.VendedorId,
                    SubordenId = dto.SubordenId,
                    ClienteId = usuarioId,
                    Calificacion = (short)dto.Calificacion,
                    Comentario = dto.Comentario?.Trim(),
                    Fecha = DateTime.UtcNow
                };
                _context.ResenasVendedor.Add(resena);
                await _context.SaveChangesAsync();

                await RecalcularPromedioVendedorAsync(suborden.VendedorId);
                await trx.CommitAsync();

                await _notif.CrearAsync(
                    suborden.Vendedor.UsuarioId,
                    TipoNotificacion.ResenaRecibida,
                    $"Recibiste una reseña de {dto.Calificacion}/5 estrellas como vendedor",
                    string.IsNullOrEmpty(dto.Comentario) ? "Sin comentario adicional." : dto.Comentario,
                    new { resenaId = resena.ResenaId });

                var cliente = await _context.Usuarios
                    .Include(u => u.Persona)
                    .FirstAsync(u => u.UsuarioId == usuarioId);

                return ResultadoOperacion<ResenaVendedorDto>.SetExito(new ResenaVendedorDto
                {
                    ResenaId = resena.ResenaId,
                    VendedorId = resena.VendedorId,
                    ClienteId = usuarioId,
                    NombreCliente = $"{cliente.Persona.Nombres} {cliente.Persona.ApellidoPaterno}".Trim(),
                    Calificacion = dto.Calificacion,
                    Comentario = resena.Comentario,
                    Fecha = resena.Fecha
                });
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                return ResultadoOperacion<ResenaVendedorDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<ResenaProductoDto>> ResponderResenaProductoAsync(
            int usuarioId, long resenaId, ResponderResenaDto dto)
        {
            try
            {
                var resena = await _context.ResenasProducto
                    .Include(r => r.Producto)
                    .FirstOrDefaultAsync(r => r.ResenaId == resenaId);

                if (resena == null)
                    return ResultadoOperacion<ResenaProductoDto>.SetError("Reseña no encontrada.");

                var vendedor = await _context.Vendedores.FirstOrDefaultAsync(v => v.UsuarioId == usuarioId);
                if (vendedor == null || resena.Producto.VendedorId != vendedor.VendedorId)
                    return ResultadoOperacion<ResenaProductoDto>.SetError("No puedes responder esta reseña.");

                if (!string.IsNullOrEmpty(resena.RespuestaVendedor))
                    return ResultadoOperacion<ResenaProductoDto>.SetError("Ya respondiste esta reseña.");

                resena.RespuestaVendedor = dto.Respuesta.Trim();
                await _context.SaveChangesAsync();

                await _notif.CrearAsync(
                    resena.ClienteId,
                    TipoNotificacion.ResenaRespondida,
                    "El vendedor respondió tu reseña",
                    dto.Respuesta,
                    new { resenaId });

                return await ObtenerResenaProductoDtoAsync(resenaId);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<ResenaProductoDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<PaginacionRespuestaDto<ResenaProductoDto>>> ListarPorProductoAsync(
            int productoId, int pagina, int tamanioPagina)
        {
            try
            {
                pagina = Math.Max(1, pagina);
                tamanioPagina = Math.Clamp(tamanioPagina, 1, 50);

                var query = _context.ResenasProducto.AsNoTracking()
                    .Include(r => r.Cliente).ThenInclude(u => u.Persona)
                    .Include(r => r.Producto)
                    .Where(r => r.ProductoId == productoId);

                var total = await query.CountAsync();
                var rows = await query.OrderByDescending(r => r.ResenaId)
                    .Skip((pagina - 1) * tamanioPagina)
                    .Take(tamanioPagina)
                    .ToListAsync();

                var items = rows.Select(r => new ResenaProductoDto
                {
                    ResenaId = r.ResenaId,
                    ProductoId = r.ProductoId,
                    NombreProducto = r.Producto.Nombre,
                    ClienteId = r.ClienteId,
                    NombreCliente = $"{r.Cliente.Persona.Nombres} {r.Cliente.Persona.ApellidoPaterno}".Trim(),
                    Calificacion = r.Calificacion,
                    Titulo = r.Titulo,
                    Comentario = r.Comentario,
                    Imagenes = string.IsNullOrEmpty(r.Imagenes)
                        ? new List<string>()
                        : JsonSerializer.Deserialize<List<string>>(r.Imagenes) ?? new List<string>(),
                    RespuestaVendedor = r.RespuestaVendedor,
                    Fecha = r.Fecha
                }).ToList();

                return ResultadoOperacion<PaginacionRespuestaDto<ResenaProductoDto>>.SetExito(
                    new PaginacionRespuestaDto<ResenaProductoDto>
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
                return ResultadoOperacion<PaginacionRespuestaDto<ResenaProductoDto>>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<PaginacionRespuestaDto<ResenaVendedorDto>>> ListarPorVendedorAsync(
            int vendedorId, int pagina, int tamanioPagina)
        {
            try
            {
                pagina = Math.Max(1, pagina);
                tamanioPagina = Math.Clamp(tamanioPagina, 1, 50);

                var query = _context.ResenasVendedor.AsNoTracking()
                    .Include(r => r.Cliente).ThenInclude(u => u.Persona)
                    .Where(r => r.VendedorId == vendedorId);

                var total = await query.CountAsync();
                var items = await query.OrderByDescending(r => r.ResenaId)
                    .Skip((pagina - 1) * tamanioPagina)
                    .Take(tamanioPagina)
                    .Select(r => new ResenaVendedorDto
                    {
                        ResenaId = r.ResenaId,
                        VendedorId = r.VendedorId,
                        ClienteId = r.ClienteId,
                        NombreCliente = r.Cliente.Persona.Nombres + " " +
                                        (r.Cliente.Persona.ApellidoPaterno ?? ""),
                        Calificacion = r.Calificacion,
                        Comentario = r.Comentario,
                        Fecha = r.Fecha
                    })
                    .ToListAsync();

                return ResultadoOperacion<PaginacionRespuestaDto<ResenaVendedorDto>>.SetExito(
                    new PaginacionRespuestaDto<ResenaVendedorDto>
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
                return ResultadoOperacion<PaginacionRespuestaDto<ResenaVendedorDto>>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<List<PendienteResenaDto>>> ObtenerPendientesAsync(int usuarioId)
        {
            try
            {
                var items = await _context.ItemsOrden.AsNoTracking()
                    .Include(i => i.Suborden).ThenInclude(s => s.Orden)
                    .Include(i => i.Suborden).ThenInclude(s => s.Vendedor)
                    .Where(i => i.Suborden.Orden.ClienteId == usuarioId &&
                                i.Suborden.Estado == TipoEstadoSuborden.Entregada)
                    .ToListAsync();

                var itemIds = items.Select(i => i.ItemOrdenId).ToList();
                var subIds = items.Select(i => i.SubordenId).Distinct().ToList();

                var reviewedProductos = await _context.ResenasProducto
                    .Where(r => itemIds.Contains(r.ItemOrdenId))
                    .Select(r => r.ItemOrdenId)
                    .ToListAsync();
                var reviewedVendedores = await _context.ResenasVendedor
                    .Where(r => subIds.Contains(r.SubordenId))
                    .Select(r => r.SubordenId)
                    .ToListAsync();

                var varianteIds = items.Where(i => i.VarianteId.HasValue).Select(i => i.VarianteId!.Value).Distinct().ToList();
                var productoPorVariante = await _context.VariantesProducto
                    .Where(v => varianteIds.Contains(v.VarianteId))
                    .ToDictionaryAsync(v => v.VarianteId, v => v.ProductoId);

                var pendientes = items
                    .Where(i => !reviewedProductos.Contains(i.ItemOrdenId) ||
                                !reviewedVendedores.Contains(i.SubordenId))
                    .Select(i => new PendienteResenaDto
                    {
                        ItemOrdenId = i.ItemOrdenId,
                        ProductoId = i.VarianteId.HasValue && productoPorVariante.TryGetValue(i.VarianteId.Value, out var pid)
                            ? pid
                            : 0,
                        NombreProducto = i.NombreProducto,
                        ImagenUrl = i.ImagenUrl,
                        SubordenId = i.SubordenId,
                        VendedorId = i.Suborden.VendedorId,
                        NombreTienda = i.Suborden.Vendedor.NombreTienda,
                        ResenoProducto = reviewedProductos.Contains(i.ItemOrdenId),
                        ResenoVendedor = reviewedVendedores.Contains(i.SubordenId)
                    })
                    .ToList();

                return ResultadoOperacion<List<PendienteResenaDto>>.SetExito(pendientes);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<List<PendienteResenaDto>>.SetError("Error: " + ex.Message);
            }
        }

        private async Task RecalcularPromedioProductoAsync(int productoId)
        {
            var stats = await _context.ResenasProducto
                .Where(r => r.ProductoId == productoId)
                .GroupBy(r => 1)
                .Select(g => new
                {
                    Promedio = (decimal)g.Average(r => (double)r.Calificacion),
                    Total = g.Count()
                })
                .FirstOrDefaultAsync();

            var producto = await _context.Productos.FirstAsync(p => p.ProductoId == productoId);
            producto.CalificacionPromedio = stats != null ? Math.Round(stats.Promedio, 2) : 0;
            producto.TotalResenas = stats?.Total ?? 0;
            await _context.SaveChangesAsync();
        }

        private async Task RecalcularPromedioVendedorAsync(int vendedorId)
        {
            var stats = await _context.ResenasVendedor
                .Where(r => r.VendedorId == vendedorId)
                .GroupBy(r => 1)
                .Select(g => new
                {
                    Promedio = (decimal)g.Average(r => (double)r.Calificacion),
                    Total = g.Count()
                })
                .FirstOrDefaultAsync();

            var vendedor = await _context.Vendedores.FirstAsync(v => v.VendedorId == vendedorId);
            vendedor.CalificacionPromedio = stats != null ? Math.Round(stats.Promedio, 2) : 0;
            await _context.SaveChangesAsync();
        }

        private async Task<ResultadoOperacion<ResenaProductoDto>> ObtenerResenaProductoDtoAsync(long resenaId)
        {
            var r = await _context.ResenasProducto.AsNoTracking()
                .Include(x => x.Cliente).ThenInclude(u => u.Persona)
                .Include(x => x.Producto)
                .FirstAsync(x => x.ResenaId == resenaId);

            return ResultadoOperacion<ResenaProductoDto>.SetExito(new ResenaProductoDto
            {
                ResenaId = r.ResenaId,
                ProductoId = r.ProductoId,
                NombreProducto = r.Producto.Nombre,
                ClienteId = r.ClienteId,
                NombreCliente = $"{r.Cliente.Persona.Nombres} {r.Cliente.Persona.ApellidoPaterno}".Trim(),
                Calificacion = r.Calificacion,
                Titulo = r.Titulo,
                Comentario = r.Comentario,
                Imagenes = string.IsNullOrEmpty(r.Imagenes)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(r.Imagenes) ?? new List<string>(),
                RespuestaVendedor = r.RespuestaVendedor,
                Fecha = r.Fecha
            });
        }
    }
}
