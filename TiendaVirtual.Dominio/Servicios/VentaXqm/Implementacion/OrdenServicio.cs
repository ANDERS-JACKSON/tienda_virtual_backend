using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.VentaXqm;
using TiendaVirtual.Dominio.Servicios.SoporteXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Servicios.VentaXqm.Implementacion
{
    /// <summary>
    /// Toma el carrito del usuario y lo materializa en una orden + N subórdenes
    /// (una por vendedor), reservando stock y dejando la orden en PendientePago.
    /// El pago real se hace en otro flujo (pasarela externa).
    /// </summary>
    public partial class OrdenServicio : IOrdenServicio
    {
        // Porcentaje fijo de comisión por ahora; cuando exista catálogo de planes
        // por vendedor, se reemplaza por la lectura del plan vigente.
        private const decimal COMISION_PORCENTAJE = 10m;

        protected readonly TiendaVirtualDbContext _context;
        private readonly INotificacionServicio _notificacionServicio;

        public OrdenServicio(TiendaVirtualDbContext context, INotificacionServicio notificacionServicio)
        {
            _context = context;
            _notificacionServicio = notificacionServicio;
        }

        public async Task<ResultadoOperacion<OrdenDto>> CrearAsync(int usuarioId, CrearOrdenDto dto)
        {
            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Datos del cliente (correo/teléfono) para snapshot
                var cliente = await _context.Usuarios
                    .Where(u => u.UsuarioId == usuarioId)
                    .Select(u => new
                    {
                        u.UsuarioId,
                        u.Correo,
                        u.PersonaId,
                        TelefonoPersona = u.Persona.Telefono,
                        NombreCliente = u.Persona.Nombres + " " + (u.Persona.ApellidoPaterno ?? "")
                    })
                    .FirstOrDefaultAsync();

                if (cliente == null)
                    return ResultadoOperacion<OrdenDto>.SetError("Usuario no encontrado.");

                // 2. Validar dirección
                var direccion = await _context.Direcciones.FirstOrDefaultAsync(d =>
                    d.DireccionId == dto.DireccionId && d.PersonaId == cliente.PersonaId);
                if (direccion == null)
                    return ResultadoOperacion<OrdenDto>.SetError("La dirección no existe o no te pertenece.");

                // 3. Cargar carrito
                var carrito = await _context.Carritos
                    .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
                if (carrito == null)
                    return ResultadoOperacion<OrdenDto>.SetError("Tu carrito está vacío.");

                var items = await _context.ItemsCarrito
                    .Include(i => i.Variante).ThenInclude(v => v.Stock)
                    .Include(i => i.Variante).ThenInclude(v => v.Producto).ThenInclude(p => p.Vendedor)
                    .Include(i => i.Variante).ThenInclude(v => v.Producto).ThenInclude(p => p.Imagenes)
                    .Where(i => i.CarritoId == carrito.CarritoId)
                    .ToListAsync();

                if (items.Count == 0)
                    return ResultadoOperacion<OrdenDto>.SetError("Tu carrito está vacío.");

                // 4. Validar métodos de envío: uno por cada vendedor del carrito
                var vendedoresEnCarrito = items.Select(i => i.Variante.Producto.VendedorId).Distinct().ToList();
                var metodosPorVendedor = dto.MetodosEnvio
                    .GroupBy(m => m.VendedorId)
                    .ToDictionary(g => g.Key, g => g.First().MetodoEnvioId);

                foreach (var vId in vendedoresEnCarrito)
                {
                    if (!metodosPorVendedor.ContainsKey(vId))
                        return ResultadoOperacion<OrdenDto>.SetError(
                            "Falta seleccionar el método de envío para uno de los vendedores.");
                }

                var idsMetodos = metodosPorVendedor.Values.Distinct().ToList();
                var metodosEnvio = await _context.MetodosEnvio
                    .Where(m => idsMetodos.Contains(m.MetodoEnvioId) && m.Activo)
                    .ToListAsync();
                if (metodosEnvio.Count != idsMetodos.Count)
                    return ResultadoOperacion<OrdenDto>.SetError("Algún método de envío no es válido.");

                // 5. Validar productos / vendedores / stock
                foreach (var i in items)
                {
                    var p = i.Variante.Producto;
                    if (p.Estado != TipoEstadoProducto.Activo)
                        return ResultadoOperacion<OrdenDto>.SetError(
                            $"El producto '{p.Nombre}' ya no está disponible.");

                    if (p.Vendedor.Estado != TipoEstadoVendedor.Activo)
                        return ResultadoOperacion<OrdenDto>.SetError(
                            $"La tienda '{p.Vendedor.NombreTienda}' ya no está activa.");

                    if (p.Tipo != TipoProducto.Patron)
                    {
                        var disponible = i.Variante.Stock?.CantidadDisponible ?? 0;
                        if (disponible < i.Cantidad)
                            return ResultadoOperacion<OrdenDto>.SetError(
                                $"Stock insuficiente para '{p.Nombre}'. Disponible: {disponible}, pediste: {i.Cantidad}.");
                    }
                }

                // 6. Ofertas vigentes (1 query para todos los productos)
                var productosIds = items.Select(i => i.Variante.ProductoId).Distinct().ToList();
                var now = DateTime.UtcNow;
                var ofertas = await _context.Ofertas
                    .Where(o => productosIds.Contains(o.ProductoId)
                                && o.Activa
                                && o.FechaInicio <= now
                                && o.FechaFin >= now)
                    .ToListAsync();

                // 7. Crear orden (cabecera)
                var numeroOrden = GenerarNumero("ORD");
                var direccionJson = JsonSerializer.Serialize(new
                {
                    direccion.Etiqueta,
                    direccion.NombreReceptor,
                    direccion.Telefono,
                    direccion.Departamento,
                    direccion.Provincia,
                    direccion.Distrito,
                    Direccion = direccion.DireccionLinea,
                    direccion.Referencia
                });

                var orden = new Orden
                {
                    NumeroOrden = numeroOrden,
                    ClienteId = usuarioId,
                    CorreoCliente = cliente.Correo,
                    TelefonoCliente = cliente.TelefonoPersona,
                    DireccionEnvio = direccionJson,
                    Estado = TipoEstadoOrden.PendientePago,
                    Fecha = DateTime.UtcNow,
                    Subtotal = 0,
                    TotalEnvio = 0,
                    TotalDescuento = 0,
                    Total = 0
                };
                _context.Ordenes.Add(orden);
                await _context.SaveChangesAsync();

                // 8. Crear una suborden por cada vendedor, con sus items
                decimal subtotalOrden = 0;
                decimal envioTotal = 0;
                var subordenesCreadas = new List<(long SubordenId, string NumeroSuborden, int VendedorId, decimal Subtotal)>();

                foreach (var grupo in items.GroupBy(i => i.Variante.Producto.VendedorId))
                {
                    var metodoId = metodosPorVendedor[grupo.Key];
                    var metodo = metodosEnvio.First(m => m.MetodoEnvioId == metodoId);

                    var suborden = new Suborden
                    {
                        NumeroSuborden = GenerarNumero("SUB"),
                        OrdenId = orden.OrdenId,
                        VendedorId = grupo.Key,
                        MetodoEnvioId = metodoId,
                        MontoEnvio = metodo.MontoBase,
                        Estado = TipoEstadoSuborden.Pendiente,
                        Subtotal = 0,
                        MontoComision = 0,
                        MontoVendedor = 0
                    };
                    _context.Subordenes.Add(suborden);
                    await _context.SaveChangesAsync();

                    decimal subtotalSuborden = 0;
                    foreach (var i in grupo)
                    {
                        var p = i.Variante.Producto;
                        var oferta = ofertas.FirstOrDefault(o => o.ProductoId == p.ProductoId);

                        var precioBase = i.Variante.Precio;
                        var precioFinal = oferta?.PrecioOferta ?? precioBase;
                        var totalLinea = Math.Round(precioFinal * i.Cantidad, 2);
                        subtotalSuborden += totalLinea;

                        var imagen = p.Imagenes.FirstOrDefault(im => im.EsPrincipal)?.Url
                                     ?? p.Imagenes.OrderBy(im => im.Orden).FirstOrDefault()?.Url;

                        _context.ItemsOrden.Add(new ItemOrden
                        {
                            SubordenId = suborden.SubordenId,
                            VarianteId = i.VarianteId,
                            NombreProducto = p.Nombre,
                            NombreVariante = i.Variante.Nombre,
                            PrecioUnitario = precioFinal,
                            Cantidad = i.Cantidad,
                            TotalLinea = totalLinea,
                            ImagenUrl = imagen,
                            TipoProducto = p.Tipo,
                            ArchivoPatronUrl = p.Tipo == TipoProducto.Patron ? p.ArchivoPatronUrl : null
                        });

                        // Reservar stock (sólo físicos)
                        if (p.Tipo != TipoProducto.Patron && i.Variante.Stock != null)
                        {
                            i.Variante.Stock.CantidadDisponible -= i.Cantidad;
                            i.Variante.Stock.CantidadReservada += i.Cantidad;
                        }
                    }

                    var comision = Math.Round(subtotalSuborden * COMISION_PORCENTAJE / 100m, 2);
                    suborden.Subtotal = subtotalSuborden;
                    suborden.MontoComision = comision;
                    suborden.MontoVendedor = subtotalSuborden - comision;

                    subordenesCreadas.Add((suborden.SubordenId, suborden.NumeroSuborden, suborden.VendedorId, suborden.Subtotal));

                    subtotalOrden += subtotalSuborden;
                    envioTotal += metodo.MontoBase;
                }

                // 9. Totales de la orden
                orden.Subtotal = subtotalOrden;
                orden.TotalEnvio = envioTotal;
                orden.TotalDescuento = 0;
                orden.Total = subtotalOrden + envioTotal;

                // 10. Vaciar carrito
                _context.ItemsCarrito.RemoveRange(items);
                carrito.FechaActualizacion = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                await _notificacionServicio.CrearAsync(
                    usuarioId,
                    TipoNotificacion.OrdenCreada,
                    $"Pedido {orden.NumeroOrden} creado",
                    $"Recibimos tu pedido. Total: S/ {orden.Total:N2}. Completa el pago para que los artesanos lo preparen.",
                    new { ordenId = orden.OrdenId, numeroOrden = orden.NumeroOrden });

                foreach (var sub in subordenesCreadas)
                {
                    var datosVendedor = await _context.Vendedores
                        .Where(v => v.VendedorId == sub.VendedorId)
                        .Select(v => new { v.UsuarioId, v.NombreTienda })
                        .FirstAsync();

                    await _notificacionServicio.CrearAsync(
                        datosVendedor.UsuarioId,
                        TipoNotificacion.SubordenRecibida,
                        "Nuevo pedido recibido",
                        $"Recibiste el pedido {sub.NumeroSuborden} por S/ {sub.Subtotal:N2}.",
                        new { subordenId = sub.SubordenId, numeroSuborden = sub.NumeroSuborden },
                        plantillaEmail: PlantillaCorreo.NuevoPedidoVendedor,
                        placeholdersEmail: new Dictionary<string, string>
                        {
                            ["vendedor"] = datosVendedor.NombreTienda,
                            ["numeroPedido"] = sub.NumeroSuborden,
                            ["nombreCliente"] = cliente.NombreCliente.Trim(),
                            ["totalPedido"] = sub.Subtotal.ToString("N2")
                        });
                }

                return await ObtenerMiOrdenAsync(usuarioId, orden.OrdenId);
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                return ResultadoOperacion<OrdenDto>.SetError("Error al crear la orden: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<PaginacionRespuestaDto<OrdenListadoDto>>> ListarMisOrdenesAsync(
            int usuarioId, int pagina, int tamanioPagina)
        {
            try
            {
                pagina = Math.Max(1, pagina);
                tamanioPagina = Math.Clamp(tamanioPagina, 1, 50);

                var baseQuery = _context.Ordenes.AsNoTracking()
                    .Where(o => o.ClienteId == usuarioId);

                var total = await baseQuery.CountAsync();

                var ordenes = await baseQuery
                    .OrderByDescending(o => o.OrdenId)
                    .Skip((pagina - 1) * tamanioPagina)
                    .Take(tamanioPagina)
                    .Select(o => new
                    {
                        o.OrdenId,
                        o.NumeroOrden,
                        o.Total,
                        o.Estado,
                        o.Fecha,
                        TotalItems = o.Subordenes.SelectMany(s => s.Items).Sum(i => (int?)i.Cantidad) ?? 0,
                        TotalVendedores = o.Subordenes.Count,
                        ImagenPrincipalUrl = o.Subordenes
                            .SelectMany(s => s.Items)
                            .Select(i => i.ImagenUrl)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                var items = ordenes.Select(o => new OrdenListadoDto
                {
                    OrdenId = o.OrdenId,
                    NumeroOrden = o.NumeroOrden,
                    Total = o.Total,
                    Estado = new EnumeracionDto((int)o.Estado, o.Estado.GetDescription()),
                    Fecha = o.Fecha,
                    TotalItems = o.TotalItems,
                    TotalVendedores = o.TotalVendedores,
                    ImagenPrincipalUrl = o.ImagenPrincipalUrl
                }).ToList();

                return ResultadoOperacion<PaginacionRespuestaDto<OrdenListadoDto>>.SetExito(
                    new PaginacionRespuestaDto<OrdenListadoDto>
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
                return ResultadoOperacion<PaginacionRespuestaDto<OrdenListadoDto>>.SetError(
                    "Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<OrdenDto>> ObtenerMiOrdenAsync(int usuarioId, long ordenId)
        {
            try
            {
                var orden = await _context.Ordenes.AsNoTracking()
                    .Include(o => o.Subordenes).ThenInclude(s => s.Vendedor)
                    .Include(o => o.Subordenes).ThenInclude(s => s.MetodoEnvio)
                    .Include(o => o.Subordenes).ThenInclude(s => s.Items)
                        .ThenInclude(it => it.Variante!).ThenInclude(v => v.Producto)
                    .Include(o => o.Subordenes).ThenInclude(s => s.Envios)
                    .FirstOrDefaultAsync(o => o.OrdenId == ordenId && o.ClienteId == usuarioId);

                if (orden == null)
                    return ResultadoOperacion<OrdenDto>.SetError("Orden no encontrada.");

                var direccion = DeserializarDireccion(orden.DireccionEnvio);

                // Sólo exponer el PDF del patrón cuando la orden ya está pagada (o más).
                var puedeDescargarPatron = (int)orden.Estado >= (int)TipoEstadoOrden.Pagada;

                var dto = new OrdenDto
                {
                    OrdenId = orden.OrdenId,
                    NumeroOrden = orden.NumeroOrden,
                    ClienteId = orden.ClienteId,
                    CorreoCliente = orden.CorreoCliente,
                    TelefonoCliente = orden.TelefonoCliente,
                    DireccionEnvio = direccion,
                    Subtotal = orden.Subtotal,
                    TotalEnvio = orden.TotalEnvio,
                    TotalDescuento = orden.TotalDescuento,
                    Total = orden.Total,
                    Estado = new EnumeracionDto((int)orden.Estado, orden.Estado.GetDescription()),
                    Fecha = orden.Fecha,
                    Subordenes = orden.Subordenes
                        .OrderBy(s => s.SubordenId)
                        .Select(s => new SubordenDto
                        {
                            SubordenId = s.SubordenId,
                            OrdenId = s.OrdenId,
                            VendedorId = s.VendedorId,
                            NombreTienda = s.Vendedor.NombreTienda,
                            SlugTienda = s.Vendedor.SlugTienda,
                            NumeroSuborden = s.NumeroSuborden,
                            MetodoEnvio = s.MetodoEnvio?.Nombre,
                            Subtotal = s.Subtotal,
                            MontoEnvio = s.MontoEnvio,
                            MontoComision = s.MontoComision,
                            MontoVendedor = s.MontoVendedor,
                            Estado = new EnumeracionDto((int)s.Estado, s.Estado.GetDescription()),
                            FechaEnvio = s.FechaEnvio,
                            FechaEntrega = s.FechaEntrega,
                            CodigoSeguimiento = s.Envios.FirstOrDefault()?.NumeroSeguimiento,
                            Items = s.Items
                                .OrderBy(i => i.ItemOrdenId)
                                .Select(i => new ItemOrdenDto
                                {
                                    ItemOrdenId = i.ItemOrdenId,
                                    SubordenId = i.SubordenId,
                                    VarianteId = i.VarianteId,
                                    ProductoId = i.Variante?.ProductoId,
                                    Slug = i.Variante?.Producto?.Slug,
                                    NombreProducto = i.NombreProducto,
                                    NombreVariante = i.NombreVariante,
                                    ImagenUrl = i.ImagenUrl,
                                    PrecioUnitario = i.PrecioUnitario,
                                    Cantidad = i.Cantidad,
                                    TotalLinea = i.TotalLinea,
                                    TipoProducto = new EnumeracionDto(
                                        (int)i.TipoProducto, i.TipoProducto.GetDescription()),
                                    ArchivoPatronUrl = puedeDescargarPatron ? i.ArchivoPatronUrl : null
                                }).ToList()
                        }).ToList()
                };

                return ResultadoOperacion<OrdenDto>.SetExito(dto);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<OrdenDto>.SetError("Error: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────
        // Cambio de estado de suborden (vendedor)
        // ─────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<bool>> CambiarEstadoSubordenAsync(
            int vendedorUsuarioId, long subordenId, TipoEstadoSuborden nuevoEstado)
        {
            try
            {
                var vendedor = await _context.Vendedores
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.UsuarioId == vendedorUsuarioId);
                if (vendedor == null)
                    return ResultadoOperacion<bool>.SetError("Vendedor no encontrado.");

                var suborden = await _context.Subordenes
                    .Include(s => s.Vendedor)
                    .Include(s => s.Envios)
                    .Include(s => s.Orden).ThenInclude(o => o.Cliente).ThenInclude(u => u.Persona)
                    .FirstOrDefaultAsync(s => s.SubordenId == subordenId && s.VendedorId == vendedor.VendedorId);

                if (suborden == null)
                    return ResultadoOperacion<bool>.SetError("Suborden no encontrada.");

                suborden.Estado = nuevoEstado;
                if (nuevoEstado == TipoEstadoSuborden.EnCamino)
                    suborden.FechaEnvio ??= DateTime.UtcNow;

                await _context.SaveChangesAsync();

                if (nuevoEstado == TipoEstadoSuborden.EnCamino)
                {
                    var nombreCliente = suborden.Orden.Cliente.Persona != null
                        ? $"{suborden.Orden.Cliente.Persona.Nombres} {suborden.Orden.Cliente.Persona.ApellidoPaterno ?? ""}".Trim()
                        : suborden.Orden.CorreoCliente;
                    var codigoSeguimiento = suborden.Envios.FirstOrDefault()?.NumeroSeguimiento;

                    await _notificacionServicio.CrearAsync(
                        suborden.Orden.ClienteId,
                        TipoNotificacion.SubordenEnCamino,
                        "Tu pedido está en camino",
                        $"El pedido {suborden.NumeroSuborden} de {suborden.Vendedor.NombreTienda} fue enviado.",
                        new { subordenId },
                        plantillaEmail: PlantillaCorreo.PedidoEnviadoCliente,
                        placeholdersEmail: new Dictionary<string, string>
                        {
                            ["cliente"] = nombreCliente,
                            ["numeroPedido"] = suborden.NumeroSuborden,
                            ["nombreTienda"] = suborden.Vendedor.NombreTienda,
                            ["codigoSeguimiento"] = string.IsNullOrEmpty(codigoSeguimiento)
                                ? "Pendiente" : codigoSeguimiento
                        });
                }

                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.SetError("Error al cambiar estado: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────
        private static string GenerarNumero(string prefijo)
        {
            // Formato: PREFIJO-yyMMdd-XXXXXX  (longitud ≤ 20)
            var sufijo = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            return $"{prefijo}-{DateTime.UtcNow:yyMMdd}-{sufijo}";
        }

        private static DireccionSnapshotDto DeserializarDireccion(string json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json))
                    return new DireccionSnapshotDto();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<DireccionSnapshotDto>(json, options)
                       ?? new DireccionSnapshotDto();
            }
            catch
            {
                return new DireccionSnapshotDto();
            }
        }
    }
}
