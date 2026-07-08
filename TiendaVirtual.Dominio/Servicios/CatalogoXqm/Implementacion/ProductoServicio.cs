using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Extensiones.CatalogoXqm;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Dominio.Servicios.SoporteXqm;
using TiendaVirtual.Dominio.Servicios.SuscripcionXqm;
using TiendaVirtual.Dominio.Utilidad;

namespace TiendaVirtual.Dominio.Servicios.CatalogoXqm.Implementacion
{
    public partial class ProductoServicio : IProductoServicio
    {
        protected readonly TiendaVirtualDbContext _context;
        private readonly ILogger<ProductoServicio> _logger;
        private readonly ISuscripcionServicio _suscripcionServicio;
        private readonly INotificacionServicio _notificacionServicio;

        public ProductoServicio(TiendaVirtualDbContext context,
            ISuscripcionServicio suscripcionServicio,
            INotificacionServicio notificacionServicio, ILogger<ProductoServicio> logger)
        {
            _logger = logger;
            _context = context;
            _suscripcionServicio = suscripcionServicio;
            _notificacionServicio = notificacionServicio;
        }

        // ─────────────────────────────────────────────────────
        // LISTADO Y DETALLE (vendedor)
        // ─────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<PaginacionRespuestaDto<ProductoDto>>> ListarMisProductosAsync(
            int usuarioId, int pagina, int tamanioPagina)
        {
            try
            {
                pagina = Math.Max(1, pagina);
                tamanioPagina = Math.Clamp(tamanioPagina, 1, 50);

                var vendedor = await ObtenerVendedorAsync(usuarioId);
                if (vendedor == null)
                    return ResultadoOperacion<PaginacionRespuestaDto<ProductoDto>>.SetError(
                        "No tienes perfil de vendedor.");

                var query = _context.Productos
                    .AsSplitQuery()
                    .Include(p => p.Categoria)
                    .Include(p => p.Variantes).ThenInclude(v => v.Stock)
                    .Include(p => p.Imagenes)
                    .Include(p => p.Ofertas)
                    .AsNoTracking()
                    .Where(p => p.VendedorId == vendedor.VendedorId);

                var total = await query.CountAsync();
                var productos = await query
                    .OrderByDescending(p => p.ProductoId)
                    .Skip((pagina - 1) * tamanioPagina)
                    .Take(tamanioPagina)
                    .ToListAsync();

                var respuesta = new PaginacionRespuestaDto<ProductoDto>
                {
                    Items = productos.Select(p => p.ToDto()).ToList(),
                    Pagina = pagina,
                    TamanioPagina = tamanioPagina,
                    TotalRegistros = total,
                    HayMas = pagina * tamanioPagina < total
                };

                return ResultadoOperacion<PaginacionRespuestaDto<ProductoDto>>.SetExito(respuesta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ProductoServicio.ListarMisProductosAsync");
                return ResultadoOperacion<PaginacionRespuestaDto<ProductoDto>>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<ProductoDto>> ObtenerMiProductoAsync(int usuarioId, int productoId)
        {
            try
            {
                var vendedor = await ObtenerVendedorAsync(usuarioId);
                if (vendedor == null)
                    return ResultadoOperacion<ProductoDto>.SetError("No tienes perfil de vendedor.");

                var producto = await CargarProductoCompletoAsync(productoId);
                if (producto == null || producto.VendedorId != vendedor.VendedorId)
                    return ResultadoOperacion<ProductoDto>.SetError("Producto no encontrado.");

                return ResultadoOperacion<ProductoDto>.SetExito(producto.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ProductoServicio.ObtenerMiProductoAsync");
                return ResultadoOperacion<ProductoDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        // ─────────────────────────────────────────────────────
        // CRUD del producto
        // ─────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<ProductoDto>> CrearAsync(int usuarioId, CrearProductoDto dto)
        {
            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                if (dto == null) return ResultadoOperacion<ProductoDto>.SetError("El DTO es nulo.");

                var vendedor = await ObtenerVendedorAsync(usuarioId);
                if (vendedor == null)
                    return ResultadoOperacion<ProductoDto>.SetError("No tienes perfil de vendedor.");

                if (vendedor.Estado != TipoEstadoVendedor.Activo)
                    return ResultadoOperacion<ProductoDto>.SetError(
                        "Tu cuenta de vendedor aún no está verificada. Envía tus documentos primero.");

                var planActivo = await _suscripcionServicio.PuedeVendedorPublicarAsync(vendedor.VendedorId);
                if (!planActivo)
                    return ResultadoOperacion<ProductoDto>.SetError(
                        "Necesitas un plan activo para crear productos. Contrata o reactiva tu suscripción en la sección de planes.");

                if (!await _context.Categorias.AnyAsync(c => c.CategoriaId == dto.CategoriaId && c.Activa))
                    return ResultadoOperacion<ProductoDto>.SetError("La categoría no existe o está inactiva.");

                var tipo = (TipoProducto)dto.Tipo.Id;

                // Validación del tipo PATRÓN
                if (tipo == TipoProducto.Patron)
                {
                    if (!vendedor.VendePatrones)
                        return ResultadoOperacion<ProductoDto>.SetError(
                            "Tu cuenta no tiene permitido vender patrones. Solicítalo al administrador.");
                    if (string.IsNullOrWhiteSpace(dto.ArchivoPatronUrl))
                        return ResultadoOperacion<ProductoDto>.SetError(
                            "Un producto de tipo PATRÓN debe tener un archivo PDF.");
                }

                // Validación de precio
                if (!dto.TieneVariantes && (dto.PrecioBase == null || dto.PrecioBase <= 0))
                    return ResultadoOperacion<ProductoDto>.SetError(
                        "Debes indicar un precio base si el producto no tiene variantes.");

                if (dto.TieneVariantes && (dto.Variantes == null || dto.Variantes.Count == 0))
                    return ResultadoOperacion<ProductoDto>.SetError(
                        "Si el producto tiene variantes, debes agregar al menos una.");

                // Validación de límite de imágenes (antes de crear nada en BD)
                var cantidadVariantes = dto.TieneVariantes
                    ? dto.Variantes?.Count ?? 0
                    : 1;
                var limiteImagenes = LimiteImagenesPorVariantes(cantidadVariantes);
                if (dto.Imagenes != null && dto.Imagenes.Count > limiteImagenes)
                    return ResultadoOperacion<ProductoDto>.SetError(
                        cantidadVariantes > 3
                            ? $"Máximo {limiteImagenes} imágenes cuando el producto tiene más de 3 variantes."
                            : $"Máximo {limiteImagenes} imágenes por producto.");

                // Slug único por vendedor
                var slug = GenerarSlugProducto(dto.Nombre, vendedor.SlugTienda);
                if (await _context.Productos.AnyAsync(p => p.Slug == slug))
                    slug = $"{slug}-{Guid.NewGuid().ToString("N")[..6]}";

                var producto = new Producto
                {
                    VendedorId = vendedor.VendedorId,
                    CategoriaId = dto.CategoriaId,
                    Nombre = dto.Nombre.Trim(),
                    Slug = slug,
                    Descripcion = dto.Descripcion,
                    DescripcionCorta = dto.DescripcionCorta,
                    Material = dto.Material,
                    Dimensiones = dto.Dimensiones,
                    TieneVariantes = dto.TieneVariantes,
                    PrecioBase = dto.PrecioBase,
                    DiasElaboracion = dto.DiasElaboracion,
                    Estado = TipoEstadoProducto.Borrador,
                    Tipo = tipo,
                    ArchivoPatronUrl = dto.ArchivoPatronUrl,
                    Vistas = 0,
                    Ventas = 0,
                    CalificacionPromedio = 0,
                    TotalResenas = 0
                };

                _context.Productos.Add(producto);
                await _context.SaveChangesAsync();

                // Imágenes
                foreach (var img in dto.Imagenes)
                {
                    _context.ImagenesProducto.Add(new ImagenProducto
                    {
                        ProductoId = producto.ProductoId,
                        Url = img.Url,
                        TextoAlt = img.TextoAlt,
                        Orden = img.Orden,
                        EsPrincipal = img.EsPrincipal
                    });
                }

                // Variantes con stock (solo si tiene variantes; sino, ignoramos lo enviado)
                if (dto.TieneVariantes)
                {
                    var variantes = new List<VarianteProducto>();
                    foreach (var v in dto.Variantes)
                    {
                        var variante = new VarianteProducto
                        {
                            ProductoId = producto.ProductoId,
                            Sku = v.Sku,
                            Nombre = v.Nombre,
                            Precio = v.Precio,
                            PesoGramos = v.PesoGramos,
                            Atributos = v.Atributos,
                            Activa = true
                        };
                        _context.VariantesProducto.Add(variante);
                        variantes.Add(variante);
                    }
                    await _context.SaveChangesAsync(); // UN solo save — EF asigna todos los IDs

                    for (int idx = 0; idx < variantes.Count; idx++)
                    {
                        _context.Stocks.Add(new Stock
                        {
                            VarianteId = variantes[idx].VarianteId,
                            CantidadDisponible = dto.Variantes[idx].CantidadInicial,
                            CantidadReservada = 0,
                            UmbralStockBajo = dto.Variantes[idx].UmbralStockBajo
                        });
                    }
                }
                else
                {
                    var defaultVariante = new VarianteProducto
                    {
                        ProductoId = producto.ProductoId,
                        Nombre = "Único",
                        Precio = dto.PrecioBase!.Value,
                        Activa = true
                    };
                    _context.VariantesProducto.Add(defaultVariante);
                    await _context.SaveChangesAsync();

                    // PATRÓN tiene stock infinito; físico usa CantidadInicial del DTO raíz
                    var cantidad = tipo == TipoProducto.Patron
                        ? 999999
                        : Math.Max(0, dto.CantidadInicial);

                    // Compatibilidad: el front puede enviar stock en variantes[0] sin cantidadInicial raíz
                    if (tipo != TipoProducto.Patron && cantidad == 0
                        && dto.Variantes is { Count: > 0 })
                    {
                        cantidad = Math.Max(0, dto.Variantes[0].CantidadInicial);
                    }

                    var umbral = dto.UmbralStockBajo > 0
                        ? dto.UmbralStockBajo
                        : dto.Variantes is { Count: > 0 } && dto.Variantes[0].UmbralStockBajo > 0
                            ? dto.Variantes[0].UmbralStockBajo
                            : 5;

                    _context.Stocks.Add(new Stock
                    {
                        VarianteId = defaultVariante.VarianteId,
                        CantidadDisponible = cantidad,
                        CantidadReservada = 0,
                        UmbralStockBajo = umbral
                    });
                }

                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                var completo = await CargarProductoCompletoAsync(producto.ProductoId);
                return ResultadoOperacion<ProductoDto>.SetExito(completo!.ToDto());
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                _logger.LogError(ex, "Error en ProductoServicio.CrearAsync");
                return ResultadoOperacion<ProductoDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<ProductoDto>> ActualizarAsync(
            int usuarioId, int productoId, ActualizarProductoDto dto)
        {
            try
            {
                if (dto == null) return ResultadoOperacion<ProductoDto>.SetError("El DTO es nulo.");

                var vendedor = await ObtenerVendedorAsync(usuarioId);
                if (vendedor == null) return ResultadoOperacion<ProductoDto>.SetError("No tienes perfil.");

                var producto = await _context.Productos.FirstOrDefaultAsync(p => p.ProductoId == productoId);
                if (producto == null || producto.VendedorId != vendedor.VendedorId)
                    return ResultadoOperacion<ProductoDto>.SetError("Producto no encontrado.");

                if (!await _context.Categorias.AnyAsync(c => c.CategoriaId == dto.CategoriaId && c.Activa))
                    return ResultadoOperacion<ProductoDto>.SetError("La categoría no existe o está inactiva.");

                if (producto.Tipo == TipoProducto.Patron && string.IsNullOrWhiteSpace(dto.ArchivoPatronUrl))
                    return ResultadoOperacion<ProductoDto>.SetError(
                        "Un producto de tipo PATRÓN debe tener un archivo PDF.");

                producto.CategoriaId = dto.CategoriaId;
                producto.Nombre = dto.Nombre.Trim();
                producto.Descripcion = dto.Descripcion;
                producto.DescripcionCorta = dto.DescripcionCorta;
                producto.Material = dto.Material;
                producto.Dimensiones = dto.Dimensiones;
                producto.PrecioBase = dto.PrecioBase;
                producto.DiasElaboracion = dto.DiasElaboracion;
                producto.ArchivoPatronUrl = dto.ArchivoPatronUrl;

                await _context.SaveChangesAsync();
                var completo = await CargarProductoCompletoAsync(productoId);
                return ResultadoOperacion<ProductoDto>.SetExito(completo!.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ProductoServicio.ActualizarAsync");
                return ResultadoOperacion<ProductoDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<bool>> EliminarAsync(int usuarioId, int productoId)
        {
            try
            {
                var vendedor = await ObtenerVendedorAsync(usuarioId);
                if (vendedor == null) return ResultadoOperacion<bool>.SetError("No tienes perfil.");

                var producto = await _context.Productos.FirstOrDefaultAsync(p => p.ProductoId == productoId);
                if (producto == null || producto.VendedorId != vendedor.VendedorId)
                    return ResultadoOperacion<bool>.SetError("Producto no encontrado.");

                // Soft delete: lo marcamos como pausado y desactivado, no lo borramos físicamente
                // porque puede tener órdenes históricas que lo referencian.
                producto.Estado = TipoEstadoProducto.Pausado;
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ProductoServicio.EliminarAsync");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<bool>> PublicarAsync(int usuarioId, int productoId)
        {
            try
            {
                var vendedor = await ObtenerVendedorAsync(usuarioId);
                if (vendedor == null) return ResultadoOperacion<bool>.SetError("No tienes perfil.");

                var producto = await _context.Productos
                    .Include(p => p.Imagenes)
                    .Include(p => p.Variantes).ThenInclude(v => v.Stock)
                    .FirstOrDefaultAsync(p => p.ProductoId == productoId);

                if (producto == null || producto.VendedorId != vendedor.VendedorId)
                    return ResultadoOperacion<bool>.SetError("Producto no encontrado.");

                if (!producto.Imagenes.Any())
                    return ResultadoOperacion<bool>.SetError("Debes agregar al menos una imagen antes de publicar.");

                var puedePublicar = await _suscripcionServicio.PuedeVendedorPublicarAsync(vendedor.VendedorId);
                if (!puedePublicar)
                    return ResultadoOperacion<bool>.SetError(
                        "Tu plan no está activo. Completa el pago o reactiva tu suscripción en 'Planes' para publicar productos.");

                var planActual = await _context.Suscripciones
                    .Include(s => s.Plan)
                    .Where(s => s.VendedorId == vendedor.VendedorId &&
                                (s.Estado == TipoEstadoSuscripcion.EnPrueba ||
                                 s.Estado == TipoEstadoSuscripcion.Activa))
                    .Select(s => s.Plan)
                    .FirstOrDefaultAsync();

                if (planActual?.MaxProductos != null)
                {
                    var publicadosActuales = await _context.Productos.CountAsync(p =>
                        p.VendedorId == vendedor.VendedorId && p.Estado == TipoEstadoProducto.Activo);
                    if (publicadosActuales >= planActual.MaxProductos)
                        return ResultadoOperacion<bool>.SetError(
                            $"Alcanzaste el máximo de {planActual.MaxProductos} productos activos de tu plan {planActual.Nombre}. " +
                            "Actualiza a un plan superior o pausa otro producto.");
                }

                if (producto.Tipo == TipoProducto.Fisico)
                {
                    var tieneStock = producto.Variantes.Any(v => v.Stock != null && v.Stock.CantidadDisponible > 0);
                    if (!tieneStock)
                        return ResultadoOperacion<bool>.SetError(
                            "Debes tener al menos una variante con stock disponible antes de publicar.");
                }

                producto.Estado = TipoEstadoProducto.Activo;
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ProductoServicio.PublicarAsync");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<bool>> PausarAsync(int usuarioId, int productoId)
        {
            try
            {
                var vendedor = await ObtenerVendedorAsync(usuarioId);
                if (vendedor == null) return ResultadoOperacion<bool>.SetError("No tienes perfil.");

                var producto = await _context.Productos.FirstOrDefaultAsync(p => p.ProductoId == productoId);
                if (producto == null || producto.VendedorId != vendedor.VendedorId)
                    return ResultadoOperacion<bool>.SetError("Producto no encontrado.");

                producto.Estado = TipoEstadoProducto.Pausado;
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ProductoServicio.PausarAsync");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        // ─────────────────────────────────────────────────────
        // VARIANTES
        // ─────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<VarianteProductoDto>> AgregarVarianteAsync(
            int usuarioId, int productoId, CrearVarianteDto dto)
        {
            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                var producto = await ValidarPropiedadProductoAsync(usuarioId, productoId);
                if (producto == null) return ResultadoOperacion<VarianteProductoDto>.SetError("Producto no encontrado.");

                var variante = new VarianteProducto
                {
                    ProductoId = productoId,
                    Sku = dto.Sku,
                    Nombre = dto.Nombre,
                    Precio = dto.Precio,
                    PesoGramos = dto.PesoGramos,
                    Atributos = dto.Atributos,
                    Activa = true
                };
                _context.VariantesProducto.Add(variante);
                await _context.SaveChangesAsync();

                _context.Stocks.Add(new Stock
                {
                    VarianteId = variante.VarianteId,
                    CantidadDisponible = dto.CantidadInicial,
                    CantidadReservada = 0,
                    UmbralStockBajo = dto.UmbralStockBajo
                });
                await _context.SaveChangesAsync();

                var activas = await _context.VariantesProducto
                    .CountAsync(v => v.ProductoId == productoId && v.Activa);
                if (activas >= 2)
                {
                    producto.TieneVariantes = true;
                    producto.PrecioBase = null;
                    await _context.SaveChangesAsync();
                }

                await trx.CommitAsync();

                var conStock = await _context.VariantesProducto.Include(v => v.Stock)
                    .FirstAsync(v => v.VarianteId == variante.VarianteId);
                return ResultadoOperacion<VarianteProductoDto>.SetExito(conStock.ToDto());
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                _logger.LogError(ex, "Error en ProductoServicio.AgregarVarianteAsync");
                return ResultadoOperacion<VarianteProductoDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<VarianteProductoDto>> ActualizarVarianteAsync(
            int usuarioId, int varianteId, ActualizarVarianteDto dto)
        {
            try
            {
                var variante = await _context.VariantesProducto
                    .Include(v => v.Stock)
                    .Include(v => v.Producto)
                    .FirstOrDefaultAsync(v => v.VarianteId == varianteId);
                if (variante == null) return ResultadoOperacion<VarianteProductoDto>.SetError("Variante no encontrada.");

                var vendedor = await ObtenerVendedorAsync(usuarioId);
                if (vendedor == null || variante.Producto.VendedorId != vendedor.VendedorId)
                    return ResultadoOperacion<VarianteProductoDto>.SetError("No tienes permiso sobre esta variante.");

                variante.Sku = dto.Sku;
                variante.Nombre = dto.Nombre;
                variante.Precio = dto.Precio;
                variante.PesoGramos = dto.PesoGramos;
                variante.Atributos = dto.Atributos;
                variante.Activa = dto.Activa;

                await _context.SaveChangesAsync();
                return ResultadoOperacion<VarianteProductoDto>.SetExito(variante.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ProductoServicio.ActualizarVarianteAsync");
                return ResultadoOperacion<VarianteProductoDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<bool>> EliminarVarianteAsync(int usuarioId, int varianteId)
        {
            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                var variante = await _context.VariantesProducto
                    .Include(v => v.Producto)
                    .FirstOrDefaultAsync(v => v.VarianteId == varianteId);
                if (variante == null) return ResultadoOperacion<bool>.SetError("Variante no encontrada.");

                var vendedor = await ObtenerVendedorAsync(usuarioId);
                if (vendedor == null || variante.Producto.VendedorId != vendedor.VendedorId)
                    return ResultadoOperacion<bool>.SetError("No tienes permiso.");

                variante.Activa = false; // soft delete

                var producto = variante.Producto;
                var restantes = await _context.VariantesProducto
                    .Where(v => v.ProductoId == producto.ProductoId && v.Activa && v.VarianteId != varianteId)
                    .ToListAsync();

                if (restantes.Count <= 1)
                {
                    producto.TieneVariantes = false;
                    if (restantes.Count == 1)
                        producto.PrecioBase = restantes[0].Precio;
                }

                await _context.SaveChangesAsync();
                await trx.CommitAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                _logger.LogError(ex, "Error en ProductoServicio.EliminarVarianteAsync");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        public async Task<ResultadoOperacion<VarianteProductoDto>> ActualizarStockAsync(
            int usuarioId, int varianteId, ActualizarStockDto dto)
        {
            try
            {
                var variante = await _context.VariantesProducto
                    .Include(v => v.Stock)
                    .Include(v => v.Producto)
                    .FirstOrDefaultAsync(v => v.VarianteId == varianteId);
                if (variante == null) return ResultadoOperacion<VarianteProductoDto>.SetError("Variante no encontrada.");

                var vendedor = await ObtenerVendedorAsync(usuarioId);
                if (vendedor == null || variante.Producto.VendedorId != vendedor.VendedorId)
                    return ResultadoOperacion<VarianteProductoDto>.SetError("No tienes permiso.");

                if (variante.Stock == null)
                {
                    _context.Stocks.Add(new Stock
                    {
                        VarianteId = varianteId,
                        CantidadDisponible = dto.CantidadDisponible,
                        CantidadReservada = 0,
                        UmbralStockBajo = dto.UmbralStockBajo
                    });
                }
                else
                {
                    variante.Stock.CantidadDisponible = dto.CantidadDisponible;
                    variante.Stock.UmbralStockBajo = dto.UmbralStockBajo;
                }

                await _context.SaveChangesAsync();
                var actualizada = await _context.VariantesProducto.Include(v => v.Stock)
                    .FirstAsync(v => v.VarianteId == varianteId);
                return ResultadoOperacion<VarianteProductoDto>.SetExito(actualizada.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ProductoServicio.ActualizarStockAsync");
                return ResultadoOperacion<VarianteProductoDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        // ─────────────────────────────────────────────────────
        // IMÁGENES
        // ─────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<ImagenProductoDto>> AgregarImagenAsync(
            int usuarioId, int productoId, CrearImagenDto dto)
        {
            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                var producto = await ValidarPropiedadProductoAsync(usuarioId, productoId);
                if (producto == null) return ResultadoOperacion<ImagenProductoDto>.SetError("Producto no encontrado.");

                var cantidadVariantes = await _context.VariantesProducto
                    .CountAsync(v => v.ProductoId == productoId);
                var limiteImagenes = LimiteImagenesPorVariantes(cantidadVariantes);
                var cantidadImagenes = await _context.ImagenesProducto
                    .CountAsync(i => i.ProductoId == productoId);
                if (cantidadImagenes >= limiteImagenes)
                    return ResultadoOperacion<ImagenProductoDto>.SetError(
                        cantidadVariantes > 3
                            ? $"Máximo {limiteImagenes} imágenes cuando el producto tiene más de 3 variantes."
                            : $"Máximo {limiteImagenes} imágenes por producto.");

                // Si esta nueva es principal, quitar el flag de las demás
                if (dto.EsPrincipal)
                {
                    var otras = await _context.ImagenesProducto.Where(i => i.ProductoId == productoId).ToListAsync();
                    foreach (var o in otras) o.EsPrincipal = false;
                }

                var imagen = new ImagenProducto
                {
                    ProductoId = productoId,
                    Url = dto.Url,
                    TextoAlt = dto.TextoAlt,
                    Orden = dto.Orden,
                    EsPrincipal = dto.EsPrincipal
                };
                _context.ImagenesProducto.Add(imagen);
                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                return ResultadoOperacion<ImagenProductoDto>.SetExito(imagen.ToDto());
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                _logger.LogError(ex, "Error en ProductoServicio.AgregarImagenAsync");
                return ResultadoOperacion<ImagenProductoDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<ImagenProductoDto>> ActualizarImagenAsync(
            int usuarioId, int imagenId, ActualizarImagenDto dto)
        {
            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                var imagen = await _context.ImagenesProducto
                    .Include(i => i.Producto)
                    .FirstOrDefaultAsync(i => i.ImagenId == imagenId);
                if (imagen == null) return ResultadoOperacion<ImagenProductoDto>.SetError("Imagen no encontrada.");

                var vendedor = await ObtenerVendedorAsync(usuarioId);
                if (vendedor == null || imagen.Producto.VendedorId != vendedor.VendedorId)
                    return ResultadoOperacion<ImagenProductoDto>.SetError("No tienes permiso.");

                if (dto.EsPrincipal && !imagen.EsPrincipal)
                {
                    var otras = await _context.ImagenesProducto
                        .Where(i => i.ProductoId == imagen.ProductoId && i.ImagenId != imagenId).ToListAsync();
                    foreach (var o in otras) o.EsPrincipal = false;
                }

                imagen.TextoAlt = dto.TextoAlt;
                imagen.Orden = dto.Orden;
                imagen.EsPrincipal = dto.EsPrincipal;

                await _context.SaveChangesAsync();
                await trx.CommitAsync();
                return ResultadoOperacion<ImagenProductoDto>.SetExito(imagen.ToDto());
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                _logger.LogError(ex, "Error en ProductoServicio.ActualizarImagenAsync");
                return ResultadoOperacion<ImagenProductoDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<bool>> EliminarImagenAsync(int usuarioId, int imagenId)
        {
            try
            {
                var imagen = await _context.ImagenesProducto
                    .Include(i => i.Producto)
                    .FirstOrDefaultAsync(i => i.ImagenId == imagenId);
                if (imagen == null) return ResultadoOperacion<bool>.SetError("Imagen no encontrada.");

                var vendedor = await ObtenerVendedorAsync(usuarioId);
                if (vendedor == null || imagen.Producto.VendedorId != vendedor.VendedorId)
                    return ResultadoOperacion<bool>.SetError("No tienes permiso.");

                _context.ImagenesProducto.Remove(imagen);
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ProductoServicio.EliminarImagenAsync");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        // ─────────────────────────────────────────────────────
        // OFERTAS
        // ─────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<OfertaDto>> CrearOfertaAsync(
            int usuarioId, int productoId, CrearOfertaDto dto)
        {
            try
            {
                var producto = await ValidarPropiedadProductoAsync(usuarioId, productoId);
                if (producto == null) return ResultadoOperacion<OfertaDto>.SetError("Producto no encontrado.");

                if (dto.PorcentajeDescuento is not > 0 && dto.PrecioOferta is not > 0)
                    return ResultadoOperacion<OfertaDto>.SetError(
                        "Debes indicar un porcentaje o un precio de oferta mayor a cero.");

                var fechaInicio = FechaHoraUtil.InicioDiaUtc(dto.FechaInicio);
                var fechaFin = FechaHoraUtil.FinDiaUtc(dto.FechaFin);

                if (fechaFin <= fechaInicio)
                    return ResultadoOperacion<OfertaDto>.SetError(
                        "La fecha de fin debe ser mayor que la fecha de inicio.");

                // Precio de referencia para convertir "precio fijo" → porcentaje.
                var productoConVariantes = await _context.Productos
                    .Include(p => p.Variantes)
                    .AsNoTracking()
                    .FirstAsync(p => p.ProductoId == productoId);

                var precioReferencia = ObtenerPrecioReferenciaOferta(productoConVariantes);
                if (precioReferencia <= 0)
                    return ResultadoOperacion<OfertaDto>.SetError(
                        "Define un precio válido (en el producto o en al menos una variante) antes de crear una oferta.");

                (decimal porcentaje, decimal precioOferta) descuento;
                try
                {
                    descuento = CalcularDescuento(
                        precioReferencia, dto.PorcentajeDescuento, dto.PrecioOferta);
                }
                catch (ArgumentException ex)
                {
                    return ResultadoOperacion<OfertaDto>.SetError(ex.Message);
                }

                var (porcentaje, precioOferta) = descuento;

                if (porcentaje <= 0)
                    return ResultadoOperacion<OfertaDto>.SetError(
                        "El descuento debe ser mayor a 0%. Si ingresaste un precio fijo, asegúrate de que sea menor al precio de la variante más barata.");

                var oferta = new Oferta
                {
                    ProductoId = productoId,
                    Nombre = dto.Nombre,
                    PorcentajeDescuento = porcentaje,
                    // Guardamos también el precio calculado (referencial); el cálculo real se
                    // hace siempre con el porcentaje sobre cada variante.
                    PrecioOferta = precioOferta,
                    FechaInicio = fechaInicio,
                    FechaFin = fechaFin,
                    Activa = true
                };
                _context.Ofertas.Add(oferta);
                await _context.SaveChangesAsync();

                return ResultadoOperacion<OfertaDto>.SetExito(oferta.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ProductoServicio.CrearOfertaAsync");
                return ResultadoOperacion<OfertaDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<OfertaDto>> ActualizarOfertaAsync(
            int usuarioId, int ofertaId, ActualizarOfertaDto dto)
        {
            try
            {
                var oferta = await _context.Ofertas
                    .Include(o => o.Producto)
                    .FirstOrDefaultAsync(o => o.OfertaId == ofertaId);
                if (oferta == null) return ResultadoOperacion<OfertaDto>.SetError("Oferta no encontrada.");

                var vendedor = await ObtenerVendedorAsync(usuarioId);
                if (vendedor == null || oferta.Producto.VendedorId != vendedor.VendedorId)
                    return ResultadoOperacion<OfertaDto>.SetError("No tienes permiso.");

                var fechaInicio = FechaHoraUtil.InicioDiaUtc(dto.FechaInicio);
                var fechaFin = FechaHoraUtil.FinDiaUtc(dto.FechaFin);

                if (fechaFin <= fechaInicio)
                    return ResultadoOperacion<OfertaDto>.SetError(
                        "La fecha de fin debe ser mayor que la fecha de inicio.");

                if (dto.PorcentajeDescuento is not > 0 && dto.PrecioOferta is not > 0)
                    return ResultadoOperacion<OfertaDto>.SetError(
                        "Debes indicar un porcentaje o un precio de oferta mayor a cero.");

                var productoConVariantes = await _context.Productos
                    .Include(p => p.Variantes)
                    .AsNoTracking()
                    .FirstAsync(p => p.ProductoId == oferta.ProductoId);

                var precioReferencia = ObtenerPrecioReferenciaOferta(productoConVariantes);
                if (precioReferencia <= 0)
                    return ResultadoOperacion<OfertaDto>.SetError(
                        "El producto no tiene un precio válido.");

                (decimal porcentaje, decimal precioOferta) descuento;
                try
                {
                    descuento = CalcularDescuento(
                        precioReferencia, dto.PorcentajeDescuento, dto.PrecioOferta);
                }
                catch (ArgumentException ex)
                {
                    return ResultadoOperacion<OfertaDto>.SetError(ex.Message);
                }

                var (porcentaje, precioOferta) = descuento;

                if (porcentaje <= 0)
                    return ResultadoOperacion<OfertaDto>.SetError(
                        "El descuento debe ser mayor a 0%. Si ingresaste un precio fijo, asegúrate de que sea menor al precio de la variante más barata.");

                oferta.Nombre = dto.Nombre;
                oferta.PorcentajeDescuento = porcentaje;
                oferta.PrecioOferta = precioOferta;
                oferta.FechaInicio = fechaInicio;
                oferta.FechaFin = fechaFin;

                await _context.SaveChangesAsync();
                return ResultadoOperacion<OfertaDto>.SetExito(oferta.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ProductoServicio.ActualizarOfertaAsync");
                return ResultadoOperacion<OfertaDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<List<OfertaDto>>> ListarOfertasAsync(int usuarioId, int productoId)
        {
            try
            {
                var producto = await ValidarPropiedadProductoAsync(usuarioId, productoId);
                if (producto == null)
                    return ResultadoOperacion<List<OfertaDto>>.SetError("Producto no encontrado.");

                var ofertas = await _context.Ofertas.AsNoTracking()
                    .Where(o => o.ProductoId == productoId)
                    .OrderByDescending(o => o.OfertaId)
                    .ToListAsync();

                return ResultadoOperacion<List<OfertaDto>>.SetExito(
                    ofertas.Select(o => o.ToDto()).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ProductoServicio.ListarOfertasAsync");
                return ResultadoOperacion<List<OfertaDto>>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<bool>> ActivarOfertaAsync(int usuarioId, int ofertaId)
        {
            try
            {
                var oferta = await _context.Ofertas
                    .Include(o => o.Producto)
                    .FirstOrDefaultAsync(o => o.OfertaId == ofertaId);
                if (oferta == null) return ResultadoOperacion<bool>.SetError("Oferta no encontrada.");

                var vendedor = await ObtenerVendedorAsync(usuarioId);
                if (vendedor == null || oferta.Producto.VendedorId != vendedor.VendedorId)
                    return ResultadoOperacion<bool>.SetError("No tienes permiso.");

                oferta.Activa = true;
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ProductoServicio.ActivarOfertaAsync");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<bool>> DesactivarOfertaAsync(int usuarioId, int ofertaId)
        {
            try
            {
                var oferta = await _context.Ofertas
                    .Include(o => o.Producto)
                    .FirstOrDefaultAsync(o => o.OfertaId == ofertaId);
                if (oferta == null) return ResultadoOperacion<bool>.SetError("Oferta no encontrada.");

                var vendedor = await ObtenerVendedorAsync(usuarioId);
                if (vendedor == null || oferta.Producto.VendedorId != vendedor.VendedorId)
                    return ResultadoOperacion<bool>.SetError("No tienes permiso.");

                oferta.Activa = false;
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ProductoServicio.DesactivarOfertaAsync");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<bool>> EliminarOfertaAsync(int usuarioId, int ofertaId)
        {
            try
            {
                var oferta = await _context.Ofertas
                    .Include(o => o.Producto)
                    .FirstOrDefaultAsync(o => o.OfertaId == ofertaId);
                if (oferta == null) return ResultadoOperacion<bool>.SetError("Oferta no encontrada.");

                var vendedor = await ObtenerVendedorAsync(usuarioId);
                if (vendedor == null || oferta.Producto.VendedorId != vendedor.VendedorId)
                    return ResultadoOperacion<bool>.SetError("No tienes permiso.");

                _context.Ofertas.Remove(oferta);
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ProductoServicio.EliminarOfertaAsync");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        // ─────────────────────────────────────────────────────
        // HELPERS PRIVADOS
        // ─────────────────────────────────────────────────────
        private async Task<Vendedor?> ObtenerVendedorAsync(int usuarioId)
        {
            return await _context.Vendedores.FirstOrDefaultAsync(v => v.UsuarioId == usuarioId);
        }

        private async Task<Producto?> CargarProductoCompletoAsync(int productoId)
        {
            return await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Variantes).ThenInclude(v => v.Stock)
                .Include(p => p.Imagenes)
                .Include(p => p.Ofertas)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductoId == productoId);
        }

        private static int LimiteImagenesPorVariantes(int cantidadVariantes) =>
            cantidadVariantes > 3 ? 6 : 4;

        private async Task<Producto?> ValidarPropiedadProductoAsync(int usuarioId, int productoId)
        {
            var vendedor = await ObtenerVendedorAsync(usuarioId);
            if (vendedor == null) return null;
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.ProductoId == productoId);
            if (producto == null || producto.VendedorId != vendedor.VendedorId) return null;
            return producto;
        }

        /// <summary>
        /// Convierte lo ingresado por el vendedor a la dupla (porcentaje, precio referencial).
        /// El porcentaje es la fuente de verdad; el precio se guarda como referencia.
        /// </summary>
        private static (decimal porcentaje, decimal precioOferta) CalcularDescuento(
            decimal precioReferencia, decimal? porcentaje, decimal? precioOferta)
        {
            if (porcentaje is > 0 and <= 99)
            {
                var pct = Math.Round(porcentaje.Value, 2);
                var precio = Math.Round(precioReferencia * (1 - pct / 100m), 2, MidpointRounding.AwayFromZero);
                return (pct, Math.Max(0.01m, precio));
            }

            if (precioOferta is > 0 && precioReferencia > 0 && precioOferta.Value < precioReferencia)
            {
                var pct = Math.Round((1 - precioOferta.Value / precioReferencia) * 100m, 2, MidpointRounding.AwayFromZero);
                pct = Math.Clamp(pct, 0.01m, 99m);
                return (pct, precioOferta.Value);
            }

            throw new ArgumentException(
                "Ingresa un porcentaje entre 1 y 99, o un precio fijo menor al precio del producto.");
        }

        /// <summary>
        /// Precio que se usa como base para convertir la oferta a porcentaje.
        /// Si el producto tiene variantes activas, usa la más barata (así el porcentaje
        /// resultante nunca deja precios negativos en ninguna variante).
        /// Si no, cae al precio base del producto.
        /// </summary>
        private static decimal ObtenerPrecioReferenciaOferta(Producto producto)
        {
            var variantes = producto.Variantes;
            if (variantes != null && variantes.Any(v => v.Activa))
                return variantes.Where(v => v.Activa).Min(v => v.Precio);
            return producto.PrecioBase ?? 0;
        }

        private static string GenerarSlugProducto(string nombre, string slugTienda)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var c in nombre.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c)) sb.Append(c);
                else if (c == ' ' || c == '-' || c == '_') sb.Append('-');
                else if (c == 'á') sb.Append('a');
                else if (c == 'é') sb.Append('e');
                else if (c == 'í') sb.Append('i');
                else if (c == 'ó') sb.Append('o');
                else if (c == 'ú' || c == 'ü') sb.Append('u');
                else if (c == 'ñ') sb.Append('n');
            }
            var slugBase = System.Text.RegularExpressions.Regex.Replace(sb.ToString(), "-+", "-").Trim('-');
            return $"{slugTienda}-{slugBase}";
        }
    }
}
