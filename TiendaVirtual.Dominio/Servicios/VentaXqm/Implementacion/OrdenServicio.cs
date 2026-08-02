using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Extensiones.VentaXqm;
using TiendaVirtual.Dominio.Modelo.VentaXqm;
using TiendaVirtual.Dominio.Servicios.SoporteXqm;
using TiendaVirtual.Dominio.Utilidad;
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
        private readonly ILogger<OrdenServicio> _logger;
        private readonly INotificacionServicio _notificacionServicio;

        public OrdenServicio(TiendaVirtualDbContext context, INotificacionServicio notificacionServicio, ILogger<OrdenServicio> logger)
        {
            _logger = logger;
            _context = context;
            _notificacionServicio = notificacionServicio;
        }

        public async Task<ResultadoOperacion<OrdenDto>> CrearAsync(Guid usuarioId, CrearOrdenDto dto)
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

                // 4. Resolver método de envío por vendedor.
                //    Regla de negocio: por defecto todos los pedidos se envían con SHALOM
                //    y el comprador paga el costo directamente al recoger en la agencia.
                //    Por eso `MetodoEnvio` en la orden es solo un dato informativo
                //    (MontoEnvio siempre = 0). El cliente ya NO elige método en el checkout,
                //    pero mantenemos compatibilidad si en el futuro llega un DTO con
                //    métodos personalizados por vendedor.
                var vendedoresEnCarrito = items.Select(i => i.Variante.Producto.VendedorId).Distinct().ToList();
                var metodosPorVendedor = dto.MetodosEnvio?
                    .GroupBy(m => m.VendedorId)
                    .ToDictionary(g => g.Key, g => g.First().MetodoEnvioId)
                    ?? new Dictionary<int, int>();

                // Fallback: cualquier vendedor sin método explícito usa el método por
                // defecto (código SHALOM). Si SHALOM no está activo, tomamos el primer
                // método activo disponible.
                int? metodoDefectoId = await _context.MetodosEnvio
                    .Where(m => m.Activo && m.Codigo == "SHALOM")
                    .Select(m => (int?)m.MetodoEnvioId)
                    .FirstOrDefaultAsync();

                if (metodoDefectoId == null)
                {
                    metodoDefectoId = await _context.MetodosEnvio
                        .Where(m => m.Activo)
                        .OrderBy(m => m.Orden)
                        .Select(m => (int?)m.MetodoEnvioId)
                        .FirstOrDefaultAsync();
                }

                if (metodoDefectoId == null)
                    return ResultadoOperacion<OrdenDto>.SetError(
                        "No hay métodos de envío configurados en el sistema.");

                foreach (var vId in vendedoresEnCarrito)
                {
                    if (!metodosPorVendedor.ContainsKey(vId))
                        metodosPorVendedor[vId] = metodoDefectoId.Value;
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
                                $"No hay stock suficiente para '{p.Nombre}'. Reduce la cantidad e intenta de nuevo.");
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

                // Conteo de variantes activas por producto: sirve para decidir si la
                // oferta puede usar `PrecioOferta` (precio fijo) como fallback cuando
                // no hay porcentaje. Solo aplica si el producto tiene UNA variante
                // real; con múltiples variantes la única forma consistente es aplicar
                // el porcentaje al precio de cada variante.
                var conteoVariantesActivas = await _context.VariantesProducto.AsNoTracking()
                    .Where(v => productosIds.Contains(v.ProductoId) && v.Activa)
                    .GroupBy(v => v.ProductoId)
                    .Select(g => new { ProductoId = g.Key, Total = g.Count() })
                    .ToDictionaryAsync(x => x.ProductoId, x => x.Total);

                // 7. Crear orden (cabecera). Snapshot COMPLETO de la dirección de envío
                //    con DNI del receptor (imprescindible para envíos por agencia).
                var numeroOrden = GenerarNumero("ORD");
                var direccionJson = JsonSerializer.Serialize(new
                {
                    direccion.Etiqueta,
                    direccion.NombreReceptor,
                    direccion.DniReceptor,
                    direccion.Telefono,
                    direccion.Departamento,
                    direccion.Provincia,
                    direccion.Distrito,
                    Direccion = direccion.DireccionLinea,
                    direccion.Referencia
                });

                var orden = new Orden
                {
                    OrdenId = Guid.NewGuid(),
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
                    DescuentoCupon = 0,
                    Total = 0
                };
                _context.Ordenes.Add(orden);
                await _context.SaveChangesAsync();

                // 8. Crear una suborden por cada vendedor, con sus items.
                //    El envío no suma al total: se paga aparte al momento del retiro.
                decimal subtotalOrden = 0;
                decimal descuentoOrden = 0;
                var subordenesCreadas = new List<(Guid SubordenId, string NumeroSuborden, int VendedorId, decimal Subtotal)>();

                foreach (var grupo in items.GroupBy(i => i.Variante.Producto.VendedorId))
                {
                    var metodoId = metodosPorVendedor[grupo.Key];

                    // El envío lo paga el comprador directamente en la agencia
                    // (Shalom / similar). NO se cobra en la orden.
                    var suborden = new Suborden
                    {
                        SubordenId = Guid.NewGuid(),
                        NumeroSuborden = GenerarNumero("SUB"),
                        OrdenId = orden.OrdenId,
                        VendedorId = grupo.Key,
                        MetodoEnvioId = metodoId,
                        MontoEnvio = 0m,
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

                        // El precio del item se calcula SIEMPRE con la misma fórmula
                        // que usan el carrito y el catálogo (PrecioOfertaUtil): así
                        // el precio que el comprador vio al agregar al carrito es el
                        // que se guarda en la orden. Con varias variantes se aplica
                        // el porcentaje al precio de la variante; sin variantes reales
                        // (una sola activa) se permite usar el `PrecioOferta` fijo
                        // como fallback.
                        var totalActivas = conteoVariantesActivas.TryGetValue(p.ProductoId, out var c) ? c : 0;
                        var soloUnaVariante = totalActivas <= 1;
                        var precioCalc = PrecioOfertaUtil.Calcular(
                            i.Variante.Precio, oferta, soloUnaVariante);

                        var precioFinal = precioCalc.PrecioActual;
                        var totalLinea = Math.Round(precioFinal * i.Cantidad, 2);
                        subtotalSuborden += totalLinea;

                        // Descuento por línea (para totalizar en la orden).
                        if (precioCalc.TieneDescuento && precioCalc.PrecioOriginal.HasValue)
                        {
                            descuentoOrden += Math.Round(
                                (precioCalc.PrecioOriginal.Value - precioFinal) * i.Cantidad, 2);
                        }

                        var imagen = p.Imagenes.FirstOrDefault(im => im.EsPrincipal)?.Url
                                     ?? p.Imagenes.OrderBy(im => im.Orden).FirstOrDefault()?.Url;

                        _context.ItemsOrden.Add(new ItemOrden
                        {
                            ItemOrdenId = Guid.NewGuid(),
                            SubordenId = suborden.SubordenId,
                            VarianteId = i.VarianteId,
                            NombreProducto = p.Nombre,
                            NombreVariante = i.Variante.Nombre,
                            PrecioUnitario = precioFinal,
                            // Se guarda solo si hubo descuento efectivo, para poder
                            // pintar el precio tachado en el histórico sin depender
                            // de la oferta actual (que puede vencer o cambiar).
                            PrecioOriginal = precioCalc.TieneDescuento
                                ? precioCalc.PrecioOriginal
                                : null,
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
                }

                // 9. Totales de la orden (envío no forma parte del cobro).
                //    `Subtotal` = suma de líneas (con ofertas). `DescuentoCupon` baja
                //    el total a pagar. `TotalDescuento` = ahorro por ofertas + cupón.
                decimal descuentoCupon = 0m;
                if (carrito.CuponPedidoId is int cuponPedidoId)
                {
                    var cupon = await _context.CuponesPedido
                        .FirstOrDefaultAsync(c => c.CuponPedidoId == cuponPedidoId);

                    if (cupon != null)
                    {
                        var ahora = DateTime.UtcNow;
                        var errorDisp = CuponPedidoUtil.ValidarDisponibilidad(cupon, ahora);
                        var errorMin = CuponPedidoUtil.ValidarMontoMinimo(cupon, subtotalOrden);

                        if (errorDisp == null && errorMin == null)
                        {
                            descuentoCupon = CuponPedidoUtil.CalcularDescuento(cupon, subtotalOrden);
                            orden.CuponPedidoId = cupon.CuponPedidoId;
                            orden.CodigoCupon = cupon.Codigo;
                            orden.DescuentoCupon = descuentoCupon;
                            cupon.UsosRealizados += 1;
                        }
                    }
                }

                orden.Subtotal = subtotalOrden;
                orden.TotalEnvio = 0m;
                orden.DescuentoCupon = descuentoCupon;
                orden.TotalDescuento = Math.Round(descuentoOrden + descuentoCupon, 2);
                orden.Total = Math.Round(Math.Max(0m, subtotalOrden - descuentoCupon), 2);

                // 10. Vaciar carrito
                _context.ItemsCarrito.RemoveRange(items);
                carrito.CuponPedidoId = null;
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
                _logger.LogError(ex, "Error en OrdenServicio.CrearAsync");
                return ResultadoOperacion<OrdenDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<PaginacionRespuestaDto<OrdenListadoDto>>> ListarMisOrdenesAsync(
            Guid usuarioId, int pagina, int tamanioPagina)
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
                _logger.LogError(ex, "Error en OrdenServicio.ListarMisOrdenesAsync");
                return ResultadoOperacion<PaginacionRespuestaDto<OrdenListadoDto>>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<OrdenDto>> ObtenerMiOrdenAsync(Guid usuarioId, Guid ordenId)
        {
            try
            {
                var orden = await _context.Ordenes.AsSplitQuery()
                    .AsNoTracking()
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
                    DescuentoCupon = orden.DescuentoCupon,
                    CodigoCupon = orden.CodigoCupon,
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
                            CodigoOrdenAgencia = s.Envios.FirstOrDefault()?.CodigoOrdenAgencia,
                            ClaveRecojo = s.Envios.FirstOrDefault()?.ClaveRecojo,
                            DetallesEnvio = s.Envios.FirstOrDefault()?.Detalles,
                            TransportistaEnvio = s.Envios.FirstOrDefault()?.Transportista,
                            ComprobanteEnvioUrl = s.Envios.FirstOrDefault()?.ComprobanteUrl,
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
                                    PrecioOriginal = i.PrecioOriginal,
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
                _logger.LogError(ex, "Error en OrdenServicio.ObtenerMiOrdenAsync");
                return ResultadoOperacion<OrdenDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        // ─────────────────────────────────────────────────────
        // Registro de envío (vendedor)
        // ─────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<EnvioDto>> RegistrarEnvioSubordenAsync(
            Guid vendedorUsuarioId, Guid subordenId, RegistrarEnvioSubordenDto dto)
        {
            try
            {
                if (dto == null)
                    return ResultadoOperacion<EnvioDto>.SetError("Datos de envío requeridos.");

                var transportista = dto.Transportista?.Trim();
                var comprobante = dto.ComprobanteUrl?.Trim();
                if (string.IsNullOrWhiteSpace(transportista))
                    return ResultadoOperacion<EnvioDto>.SetError("Indica el transportista o courier.");
                if (string.IsNullOrWhiteSpace(comprobante))
                    return ResultadoOperacion<EnvioDto>.SetError("Sube el comprobante de envío.");

                var vendedor = await _context.Vendedores
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.UsuarioId == vendedorUsuarioId);
                if (vendedor == null)
                    return ResultadoOperacion<EnvioDto>.SetError("Vendedor no encontrado.");

                var suborden = await _context.Subordenes
                    .Include(s => s.Vendedor)
                    .Include(s => s.MetodoEnvio)
                    .Include(s => s.Envios)
                    .Include(s => s.Orden).ThenInclude(o => o.Cliente).ThenInclude(u => u.Persona)
                    .FirstOrDefaultAsync(s => s.SubordenId == subordenId && s.VendedorId == vendedor.VendedorId);

                if (suborden == null)
                    return ResultadoOperacion<EnvioDto>.SetError("Suborden no encontrada.");

                if (suborden.Estado != TipoEstadoSuborden.EnPreparacion)
                {
                    return ResultadoOperacion<EnvioDto>.SetError(
                        suborden.Estado == TipoEstadoSuborden.Pendiente
                            ? "El pedido aún no está pagado. Espera a que el cliente complete el pago."
                            : "Solo puedes registrar envío de pedidos en preparación.");
                }

                if (suborden.Envios.Any())
                    return ResultadoOperacion<EnvioDto>.SetError("Este pedido ya tiene un envío registrado.");

                if (string.Equals(suborden.MetodoEnvio?.Codigo, "RECOJO", StringComparison.OrdinalIgnoreCase))
                {
                    return ResultadoOperacion<EnvioDto>.SetError(
                        "Este pedido es recojo en tienda. Márcalo como listo para recoger, sin registrar envío.");
                }

                var metodoCodigo = suborden.MetodoEnvio?.Codigo?.Trim().ToUpperInvariant();
                var codigoOrdenAgencia = dto.CodigoOrdenAgencia?.Trim();
                var numeroSeguimiento = dto.NumeroSeguimiento?.Trim();
                var claveRecojo = dto.ClaveRecojo?.Trim();
                var detalles = dto.Detalles?.Trim();

                if (metodoCodigo == "SHALOM")
                {
                    if (string.IsNullOrWhiteSpace(numeroSeguimiento))
                        return ResultadoOperacion<EnvioDto>.SetError("Indica el número de seguimiento.");
                    if (string.IsNullOrWhiteSpace(codigoOrdenAgencia))
                        return ResultadoOperacion<EnvioDto>.SetError("Indica el código de orden de la agencia.");
                    if (string.IsNullOrWhiteSpace(claveRecojo))
                        return ResultadoOperacion<EnvioDto>.SetError("Indica la clave de recojo para el cliente.");
                }

                var envio = new Envio
                {
                    EnvioId = Guid.NewGuid(),
                    SubordenId = suborden.SubordenId,
                    Transportista = transportista,
                    CodigoOrdenAgencia = string.IsNullOrWhiteSpace(codigoOrdenAgencia)
                        ? null
                        : codigoOrdenAgencia,
                    NumeroSeguimiento = string.IsNullOrWhiteSpace(numeroSeguimiento)
                        ? null
                        : numeroSeguimiento,
                    ClaveRecojo = string.IsNullOrWhiteSpace(claveRecojo)
                        ? null
                        : claveRecojo,
                    Detalles = string.IsNullOrWhiteSpace(detalles)
                        ? null
                        : detalles,
                    ComprobanteUrl = comprobante,
                    MontoComprobante = dto.MontoComprobante
                };

                _context.Envios.Add(envio);
                suborden.Estado = TipoEstadoSuborden.EnCamino;
                suborden.FechaEnvio = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var nombreCliente = suborden.Orden.Cliente.Persona != null
                    ? $"{suborden.Orden.Cliente.Persona.Nombres} {suborden.Orden.Cliente.Persona.ApellidoPaterno ?? ""}".Trim()
                    : suborden.Orden.CorreoCliente;

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
                        ["codigoSeguimiento"] = string.IsNullOrEmpty(envio.NumeroSeguimiento)
                            ? "Pendiente"
                            : envio.NumeroSeguimiento,
                        ["codigoOrdenAgencia"] = envio.CodigoOrdenAgencia ?? "",
                        ["claveRecojo"] = envio.ClaveRecojo ?? "",
                        ["detallesEnvio"] = envio.Detalles ?? ""
                    });

                return ResultadoOperacion<EnvioDto>.SetExito(envio.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en OrdenServicio.RegistrarEnvioSubordenAsync");
                return ResultadoOperacion<EnvioDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        // ─────────────────────────────────────────────────────
        // Listo para recoger (recojo en tienda)
        // ─────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<bool>> MarcarListoParaRecogerAsync(
            Guid vendedorUsuarioId, Guid subordenId, MarcarListoParaRecogerDto? dto)
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
                    .Include(s => s.MetodoEnvio)
                    .Include(s => s.Envios)
                    .Include(s => s.Orden).ThenInclude(o => o.Cliente).ThenInclude(u => u.Persona)
                    .FirstOrDefaultAsync(s => s.SubordenId == subordenId && s.VendedorId == vendedor.VendedorId);

                if (suborden == null)
                    return ResultadoOperacion<bool>.SetError("Suborden no encontrada.");

                if (!string.Equals(suborden.MetodoEnvio?.Codigo, "RECOJO", StringComparison.OrdinalIgnoreCase))
                {
                    return ResultadoOperacion<bool>.SetError(
                        "Solo aplica a pedidos con recojo en tienda.");
                }

                if (suborden.Estado != TipoEstadoSuborden.EnPreparacion)
                {
                    return ResultadoOperacion<bool>.SetError(
                        suborden.Estado == TipoEstadoSuborden.Pendiente
                            ? "El pedido aún no está pagado."
                            : "Solo puedes marcar listo para recoger pedidos en preparación.");
                }

                var detalles = dto?.Detalles?.Trim();

                suborden.Estado = TipoEstadoSuborden.EnCamino;
                suborden.FechaEnvio = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var nombreCliente = suborden.Orden.Cliente.Persona != null
                    ? $"{suborden.Orden.Cliente.Persona.Nombres} {suborden.Orden.Cliente.Persona.ApellidoPaterno ?? ""}".Trim()
                    : suborden.Orden.CorreoCliente;

                var mensajeDetalle = string.IsNullOrWhiteSpace(detalles)
                    ? $"Tu pedido {suborden.NumeroSuborden} de {suborden.Vendedor.NombreTienda} está listo para recoger en tienda."
                    : $"Tu pedido {suborden.NumeroSuborden} de {suborden.Vendedor.NombreTienda} está listo para recoger. {detalles}";

                await _notificacionServicio.CrearAsync(
                    suborden.Orden.ClienteId,
                    TipoNotificacion.SubordenEnCamino,
                    "Tu pedido está listo para recoger",
                    mensajeDetalle,
                    new { subordenId, recojoEnTienda = true, detalles });

                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en OrdenServicio.MarcarListoParaRecogerAsync");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        // ─────────────────────────────────────────────────────
        // Cambio de estado de suborden (vendedor)
        // ─────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<bool>> CambiarEstadoSubordenAsync(
            Guid vendedorUsuarioId, Guid subordenId, TipoEstadoSuborden nuevoEstado)
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

                var estadoAnterior = suborden.Estado;

                if (nuevoEstado == TipoEstadoSuborden.EnCamino)
                {
                    return ResultadoOperacion<bool>.SetError(
                        "Registra el envío con el comprobante para marcar el pedido en camino.");
                }

                if (nuevoEstado == TipoEstadoSuborden.Entregada)
                {
                    if (estadoAnterior != TipoEstadoSuborden.EnCamino)
                        return ResultadoOperacion<bool>.SetError(
                            estadoAnterior == TipoEstadoSuborden.EnPreparacion
                                ? "Primero registra el envío o marca el pedido como listo para recoger."
                                : "Solo puedes marcar como entregado un pedido que ya está en camino o listo para recoger.");
                }
                else
                {
                    return ResultadoOperacion<bool>.SetError("Cambio de estado no permitido.");
                }

                suborden.Estado = nuevoEstado;
                suborden.FechaEntrega = DateTime.UtcNow;

                if (estadoAnterior != TipoEstadoSuborden.Entregada)
                {
                    await _context.Vendedores
                        .Where(v => v.VendedorId == suborden.VendedorId)
                        .ExecuteUpdateAsync(s => s.SetProperty(v => v.TotalVentas, v => v.TotalVentas + 1));
                }

                await _context.SaveChangesAsync();

                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en OrdenServicio.CambiarEstadoSubordenAsync");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
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
