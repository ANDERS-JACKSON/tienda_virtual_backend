using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Extensiones.VendedorXqm;
using TiendaVirtual.Dominio.Extensiones.VentaXqm;
using TiendaVirtual.Dominio.Servicios.SoporteXqm;
using TiendaVirtual.Dominio.Servicios.SuscripcionXqm;
using TiendaVirtual.Dominio.Servicios.SuscripcionXqm.Implementacion;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Servicios.VendedorXqm.Implementacion
{
    public class VendedorServicio : IVendedorServicio
    {
        protected readonly TiendaVirtualDbContext _context;
        private readonly INotificacionServicio _notificacionServicio;
        private readonly ISuscripcionServicio _suscripcionServicio;

        public VendedorServicio(
            TiendaVirtualDbContext context,
            INotificacionServicio notificacionServicio,
            ISuscripcionServicio suscripcionServicio)
        {
            _context = context;
            _notificacionServicio = notificacionServicio;
            _suscripcionServicio = suscripcionServicio;
        }

        // ─────────────────────────────────────────────────────────────
        // PERFIL PROPIO
        // ─────────────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<VendedorPerfilDto>> ObtenerMiPerfilAsync(int usuarioId)
        {
            try
            {
                var vendedor = await _context.Vendedores
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.UsuarioId == usuarioId);

                if (vendedor == null)
                    return ResultadoOperacion<VendedorPerfilDto>.SetError("No tienes un perfil de vendedor.");

                var dto = await ConstruirPerfilConMetricasAsync(vendedor);
                return ResultadoOperacion<VendedorPerfilDto>.SetExito(dto);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<VendedorPerfilDto>.SetError("Error al obtener el perfil: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<ElegibilidadCreacionProductoDto>> ObtenerElegibilidadCreacionProductoAsync(int usuarioId)
        {
            try
            {
                var vendedor = await _context.Vendedores
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.UsuarioId == usuarioId);

                if (vendedor == null)
                    return ResultadoOperacion<ElegibilidadCreacionProductoDto>.SetError(
                        "No tienes un perfil de vendedor.");

                var cuentaVerificada = vendedor.Estado == TipoEstadoVendedor.Activo;
                var planActivo = cuentaVerificada
                    && await _suscripcionServicio.PuedeVendedorPublicarAsync(vendedor.VendedorId);

                var dto = new ElegibilidadCreacionProductoDto
                {
                    CuentaVerificada = cuentaVerificada,
                    PlanActivo = planActivo,
                    PuedeCrearProducto = cuentaVerificada && planActivo,
                };

                if (dto.PuedeCrearProducto)
                {
                    dto.CodigoBloqueo = "OK";
                    dto.Mensaje = "Puedes crear productos.";
                    return ResultadoOperacion<ElegibilidadCreacionProductoDto>.SetExito(dto);
                }

                if (!cuentaVerificada)
                {
                    dto.CodigoBloqueo = "VERIFICACION";
                    dto.Mensaje = vendedor.Estado switch
                    {
                        TipoEstadoVendedor.PendienteVerificacion =>
                            "Tu cuenta de vendedor aún no está verificada. Envía tus documentos primero.",
                        TipoEstadoVendedor.Rechazado =>
                            "Tu verificación fue rechazada. Revisa el detalle y reenvía tus documentos.",
                        TipoEstadoVendedor.Suspendido =>
                            "Tu cuenta de vendedor está suspendida. Contacta al soporte.",
                        _ => "Tu cuenta de vendedor no está habilitada para crear productos.",
                    };
                    return ResultadoOperacion<ElegibilidadCreacionProductoDto>.SetExito(dto);
                }

                dto.CodigoBloqueo = "PLAN";
                dto.Mensaje =
                    "Necesitas un plan activo para crear productos. Contrata o reactiva tu suscripción en la sección de planes.";
                return ResultadoOperacion<ElegibilidadCreacionProductoDto>.SetExito(dto);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<ElegibilidadCreacionProductoDto>.SetError(
                    "Error al consultar elegibilidad: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<VendedorPerfilDto>> ActualizarMiPerfilAsync(int usuarioId, ActualizarPerfilVendedorDto dto)
        {
            try
            {
                if (dto == null)
                    return ResultadoOperacion<VendedorPerfilDto>.SetError("El DTO es nulo.");

                var vendedor = await _context.Vendedores
                    .FirstOrDefaultAsync(v => v.UsuarioId == usuarioId);

                if (vendedor == null)
                    return ResultadoOperacion<VendedorPerfilDto>.SetError("No tienes un perfil de vendedor.");

                // NOTA: el slug NO se actualiza. Una vez asignado al registrarse, queda fijo
                // porque cambia las URLs públicas de la tienda y rompe links externos.
                vendedor.NombreTienda = dto.NombreTienda?.Trim() ?? vendedor.NombreTienda;
                vendedor.Biografia = dto.Biografia;
                vendedor.NumeroYape = dto.NumeroYape;

                // Solo actualizar imágenes si vienen en el body (null = no enviado, conservar valor actual).
                if (dto.LogoUrl != null)
                    vendedor.LogoUrl = string.IsNullOrWhiteSpace(dto.LogoUrl) ? null : dto.LogoUrl.Trim();
                if (dto.BannerUrl != null)
                    vendedor.BannerUrl = string.IsNullOrWhiteSpace(dto.BannerUrl) ? null : dto.BannerUrl.Trim();

                await _context.SaveChangesAsync();
                return ResultadoOperacion<VendedorPerfilDto>.SetExito(
                    await ConstruirPerfilConMetricasAsync(vendedor));
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<VendedorPerfilDto>.SetError("Error al actualizar el perfil: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<VendedorPerfilDto>> ActualizarImagenesPerfilAsync(
            int usuarioId, ActualizarImagenesPerfilVendedorDto dto)
        {
            try
            {
                if (dto == null)
                    return ResultadoOperacion<VendedorPerfilDto>.SetError("El DTO es nulo.");

                var tieneLogo = !string.IsNullOrWhiteSpace(dto.LogoUrl);
                var tieneBanner = !string.IsNullOrWhiteSpace(dto.BannerUrl);
                if (!tieneLogo && !tieneBanner)
                    return ResultadoOperacion<VendedorPerfilDto>.SetError("Indica al menos logo o banner.");

                var vendedor = await _context.Vendedores
                    .FirstOrDefaultAsync(v => v.UsuarioId == usuarioId);

                if (vendedor == null)
                    return ResultadoOperacion<VendedorPerfilDto>.SetError("No tienes un perfil de vendedor.");

                if (tieneLogo)
                    vendedor.LogoUrl = dto.LogoUrl!.Trim();
                if (tieneBanner)
                    vendedor.BannerUrl = dto.BannerUrl!.Trim();

                await _context.SaveChangesAsync();
                return ResultadoOperacion<VendedorPerfilDto>.SetExito(
                    await ConstruirPerfilConMetricasAsync(vendedor));
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<VendedorPerfilDto>.SetError(
                    "Error al actualizar las imágenes del perfil: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // SOLICITUD DE VERIFICACIÓN
        // ─────────────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<SolicitudVerificacionDto>> EnviarSolicitudVerificacionAsync(int usuarioId, EnviarSolicitudVerificacionDto dto)
        {
            try
            {
                if (dto == null)
                    return ResultadoOperacion<SolicitudVerificacionDto>.SetError("El DTO es nulo.");

                var vendedor = await _context.Vendedores
                    .Include(v => v.Usuario)
                    .FirstOrDefaultAsync(v => v.UsuarioId == usuarioId);

                if (vendedor == null)
                    return ResultadoOperacion<SolicitudVerificacionDto>.SetError("No tienes un perfil de vendedor.");

                if (string.IsNullOrWhiteSpace(dto.DocumentoFrenteUrl))
                    return ResultadoOperacion<SolicitudVerificacionDto>.SetError("El documento (frente) es obligatorio.");

                if (vendedor.Estado == TipoEstadoVendedor.Activo)
                    return ResultadoOperacion<SolicitudVerificacionDto>.SetError("Tu cuenta ya está verificada.");

                if (vendedor.Estado == TipoEstadoVendedor.Suspendido)
                    return ResultadoOperacion<SolicitudVerificacionDto>.SetError("Tu cuenta está suspendida. Contacta al administrador.");

                // ¿Tiene una solicitud abierta sin resolver?
                var solicitudAbierta = await _context.SolicitudesVerificacion
                    .AnyAsync(s => s.VendedorId == vendedor.VendedorId &&
                                   s.Estado == TipoEstadoSolicitudVerificacion.Enviada);

                if (solicitudAbierta)
                    return ResultadoOperacion<SolicitudVerificacionDto>.SetError(
                        "Ya tienes una solicitud en revisión. Espera el resultado antes de enviar otra.");

                var solicitud = new SolicitudVerificacion
                {
                    VendedorId = vendedor.VendedorId,
                    Estado = TipoEstadoSolicitudVerificacion.Enviada,
                    DocumentoFrenteUrl = dto.DocumentoFrenteUrl,
                    DocumentoReversoUrl = dto.DocumentoReversoUrl,
                    SelfieDocumentoUrl = dto.SelfieDocumentoUrl,
                    FotosProductos = dto.FotosProductos,
                    FechaEnvio = DateTime.UtcNow
                };

                _context.SolicitudesVerificacion.Add(solicitud);
                await _context.SaveChangesAsync();

                solicitud.Vendedor = vendedor;
                return ResultadoOperacion<SolicitudVerificacionDto>.SetExito(solicitud.ToDto());
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<SolicitudVerificacionDto>.SetError("Error al enviar la solicitud: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<SolicitudVerificacionDto?>> ObtenerMiSolicitudActualAsync(int usuarioId)
        {
            try
            {
                var vendedor = await _context.Vendedores
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.UsuarioId == usuarioId);

                if (vendedor == null)
                    return ResultadoOperacion<SolicitudVerificacionDto?>.SetError("No tienes un perfil de vendedor.");

                var solicitud = await _context.SolicitudesVerificacion
                    .Include(s => s.Vendedor).ThenInclude(v => v.Usuario)
                    .AsNoTracking()
                    .Where(s => s.VendedorId == vendedor.VendedorId)
                    .OrderByDescending(s => s.FechaEnvio)
                    .FirstOrDefaultAsync();

                return ResultadoOperacion<SolicitudVerificacionDto?>.SetExito(solicitud?.ToDto());
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<SolicitudVerificacionDto?>.SetError("Error: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // RESOLUCIÓN (ADMIN / VERIFICADOR)
        // ─────────────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<PaginacionRespuestaDto<SolicitudVerificacionDto>>> ListarSolicitudesPendientesAsync(int pagina, int tamanioPagina)
        {
            try
            {
                pagina = Math.Max(1, pagina);
                tamanioPagina = Math.Clamp(tamanioPagina, 1, 50);

                var query = _context.SolicitudesVerificacion
                    .Include(s => s.Vendedor).ThenInclude(v => v.Usuario)
                    .AsNoTracking()
                    .Where(s => s.Estado == TipoEstadoSolicitudVerificacion.Enviada)
                    .OrderBy(s => s.FechaEnvio); // las más antiguas primero (FIFO)

                var total = await query.CountAsync();
                var items = await query
                    .Skip((pagina - 1) * tamanioPagina)
                    .Take(tamanioPagina)
                    .ToListAsync();

                var resultado = new PaginacionRespuestaDto<SolicitudVerificacionDto>
                {
                    Items = items.Select(s => s.ToDto()).ToList(),
                    Pagina = pagina,
                    TamanioPagina = tamanioPagina,
                    TotalRegistros = total,
                    HayMas = pagina * tamanioPagina < total
                };

                return ResultadoOperacion<PaginacionRespuestaDto<SolicitudVerificacionDto>>.SetExito(resultado);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<PaginacionRespuestaDto<SolicitudVerificacionDto>>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> AprobarSolicitudAsync(int solicitudId, int verificadorUsuarioId, ResolverSolicitudDto dto)
        {
            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                var solicitud = await _context.SolicitudesVerificacion
                    .Include(s => s.Vendedor)
                    .FirstOrDefaultAsync(s => s.SolicitudId == solicitudId);

                if (solicitud == null)
                    return ResultadoOperacion<bool>.SetError("La solicitud no existe.");

                if (solicitud.Estado != TipoEstadoSolicitudVerificacion.Enviada)
                    return ResultadoOperacion<bool>.SetError("Esta solicitud ya fue resuelta.");

                solicitud.Estado = TipoEstadoSolicitudVerificacion.Aprobada;
                solicitud.VerificadorId = verificadorUsuarioId;
                solicitud.NotasRevisor = dto?.NotasRevisor;
                solicitud.FechaRevision = DateTime.UtcNow;

                solicitud.Vendedor.Estado = TipoEstadoVendedor.Activo;

                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                await _notificacionServicio.CrearAsync(
                    solicitud.Vendedor.UsuarioId,
                    TipoNotificacion.VerificacionAprobada,
                    "¡Tu cuenta fue verificada!",
                    "Ya puedes publicar productos y vender en Artesanías.",
                    null,
                    plantillaEmail: PlantillaCorreo.VerificacionResultado,
                    placeholdersEmail: new Dictionary<string, string>
                    {
                        ["vendedor"] = solicitud.Vendedor.NombreTienda,
                        ["resultado"] = "Aprobada",
                        ["titulo"] = "¡Tu cuenta fue verificada!",
                        ["mensaje"] = "Felicitaciones, tu cuenta de artesano fue aprobada. " +
                                      "Ya puedes publicar productos e iniciar tu suscripción " +
                                      "para empezar a vender en Artesanías Perú."
                    });

                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                return ResultadoOperacion<bool>.SetError("Error al aprobar: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> RechazarSolicitudAsync(int solicitudId, int verificadorUsuarioId, ResolverSolicitudDto dto)
        {
            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.MotivoRechazo))
                    return ResultadoOperacion<bool>.SetError("Debes indicar el motivo del rechazo.");

                var solicitud = await _context.SolicitudesVerificacion
                    .Include(s => s.Vendedor)
                    .FirstOrDefaultAsync(s => s.SolicitudId == solicitudId);

                if (solicitud == null)
                    return ResultadoOperacion<bool>.SetError("La solicitud no existe.");

                if (solicitud.Estado != TipoEstadoSolicitudVerificacion.Enviada)
                    return ResultadoOperacion<bool>.SetError("Esta solicitud ya fue resuelta.");

                solicitud.Estado = TipoEstadoSolicitudVerificacion.Rechazada;
                solicitud.VerificadorId = verificadorUsuarioId;
                solicitud.NotasRevisor = dto.NotasRevisor;
                solicitud.MotivoRechazo = dto.MotivoRechazo;
                solicitud.FechaRevision = DateTime.UtcNow;

                // El vendedor queda en PENDIENTE_VERIFICACION para que pueda reenviar
                solicitud.Vendedor.Estado = TipoEstadoVendedor.PendienteVerificacion;

                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                await _notificacionServicio.CrearAsync(
                    solicitud.Vendedor.UsuarioId,
                    TipoNotificacion.VerificacionRechazada,
                    "Tu solicitud de verificación fue rechazada",
                    $"Motivo: {dto.MotivoRechazo}. Puedes corregir y reenviar la solicitud.",
                    null,
                    plantillaEmail: PlantillaCorreo.VerificacionResultado,
                    placeholdersEmail: new Dictionary<string, string>
                    {
                        ["vendedor"] = solicitud.Vendedor.NombreTienda,
                        ["resultado"] = "Rechazada",
                        ["titulo"] = "Tu verificación fue rechazada",
                        ["mensaje"] = $"Motivo: {dto.MotivoRechazo}. " +
                                      "Por favor revisa los datos enviados y vuelve a enviar tu solicitud " +
                                      "desde tu panel de vendedor."
                    });

                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                return ResultadoOperacion<bool>.SetError("Error al rechazar: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // LISTADO PÚBLICO DE TIENDAS
        // ─────────────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<PaginacionRespuestaDto<TiendaPublicaDto>>> ListarTiendasPublicasAsync(int pagina, int tamanioPagina, string? busqueda)
        {
            try
            {
                pagina = Math.Max(1, pagina);
                tamanioPagina = Math.Clamp(tamanioPagina, 1, 30);
                var now = DateTime.UtcNow;

                var query = _context.Vendedores
                    .AsNoTracking()
                    .Where(v => v.Estado == TipoEstadoVendedor.Activo)
                    .ConPlanActivo(_context, now);

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    var b = busqueda.ToLower();
                    query = query.Where(v => v.NombreTienda.ToLower().Contains(b)
                                          || v.SlugTienda.ToLower().Contains(b));
                }

                var total = await query.CountAsync();

                // Trae los vendedores + cuenta sus productos activos en una sola query
                var resultados = await query
                    .OrderByDescending(v => _context.Subordenes.Count(s =>
                        s.VendedorId == v.VendedorId &&
                        s.Estado == TipoEstadoSuborden.Entregada))
                    .ThenByDescending(v => v.CalificacionPromedio)
                    .Skip((pagina - 1) * tamanioPagina)
                    .Take(tamanioPagina)
                    .Select(v => new
                    {
                        Vendedor = v,
                        TotalProductos = _context.Productos.Count(p =>
                            p.VendedorId == v.VendedorId &&
                            p.Estado == TipoEstadoProducto.Activo),
                        TotalVentas = _context.Subordenes.Count(s =>
                            s.VendedorId == v.VendedorId &&
                            s.Estado == TipoEstadoSuborden.Entregada)
                    })
                    .ToListAsync();

                var resultado = new PaginacionRespuestaDto<TiendaPublicaDto>
                {
                    Items = resultados.Select(r => r.Vendedor.ToTiendaPublicaDto(
                        r.TotalProductos, r.TotalVentas)).ToList(),
                    Pagina = pagina,
                    TamanioPagina = tamanioPagina,
                    TotalRegistros = total,
                    HayMas = pagina * tamanioPagina < total
                };

                return ResultadoOperacion<PaginacionRespuestaDto<TiendaPublicaDto>>.SetExito(resultado);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<PaginacionRespuestaDto<TiendaPublicaDto>>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<TiendaPublicaDto>> ObtenerTiendaPorSlugAsync(string slug)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(slug))
                    return ResultadoOperacion<TiendaPublicaDto>.SetError("Slug requerido.");

                var now = DateTime.UtcNow;
                var resultado = await _context.Vendedores
                    .AsNoTracking()
                    .Where(v => v.SlugTienda == slug.ToLower() &&
                                v.Estado == TipoEstadoVendedor.Activo)
                    .ConPlanActivo(_context, now)
                    .Select(v => new
                    {
                        Vendedor = v,
                        TotalProductos = _context.Productos.Count(p =>
                            p.VendedorId == v.VendedorId &&
                            p.Estado == TipoEstadoProducto.Activo),
                        TotalVentas = _context.Subordenes.Count(s =>
                            s.VendedorId == v.VendedorId &&
                            s.Estado == TipoEstadoSuborden.Entregada)
                    })
                    .FirstOrDefaultAsync();

                if (resultado == null)
                    return ResultadoOperacion<TiendaPublicaDto>.SetError("Tienda no encontrada.");

                return ResultadoOperacion<TiendaPublicaDto>.SetExito(
                    resultado.Vendedor.ToTiendaPublicaDto(resultado.TotalProductos, resultado.TotalVentas));
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<TiendaPublicaDto>.SetError("Error: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // HISTORIAS PÚBLICAS (BIOGRAFÍAS DE VENDEDORES)
        // ─────────────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<PaginacionRespuestaDto<HistoriaPublicaListadoDto>>> ListarHistoriasPublicasAsync(
            int pagina, int tamanioPagina, string? busqueda)
        {
            try
            {
                pagina = Math.Max(1, pagina);
                tamanioPagina = Math.Clamp(tamanioPagina, 1, 30);
                var now = DateTime.UtcNow;

                var query = QueryVendedoresConHistoriaPublica(now);

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    var b = busqueda.Trim().ToLower();
                    query = query.Where(v => v.NombreTienda.ToLower().Contains(b)
                                          || v.SlugTienda.ToLower().Contains(b)
                                          || (v.Biografia != null && v.Biografia.ToLower().Contains(b)));
                }

                var total = await query.CountAsync();

                var vendedores = await query
                    .OrderByDescending(v => v.TotalVentas)
                    .ThenByDescending(v => v.CalificacionPromedio)
                    .Skip((pagina - 1) * tamanioPagina)
                    .Take(tamanioPagina)
                    .ToListAsync();

                var vendedorIds = vendedores.Select(v => v.VendedorId).ToList();
                var categorias = await ObtenerCategoriasPrincipalesPorVendedorAsync(vendedorIds);

                var items = vendedores
                    .Select(v => v.ToHistoriaPublicaListadoDto(
                        categorias.GetValueOrDefault(v.VendedorId)))
                    .ToList();

                var resultado = new PaginacionRespuestaDto<HistoriaPublicaListadoDto>
                {
                    Items = items,
                    Pagina = pagina,
                    TamanioPagina = tamanioPagina,
                    TotalRegistros = total,
                    HayMas = pagina * tamanioPagina < total
                };

                return ResultadoOperacion<PaginacionRespuestaDto<HistoriaPublicaListadoDto>>.SetExito(resultado);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<PaginacionRespuestaDto<HistoriaPublicaListadoDto>>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<HistoriaPublicaDetalleDto>> ObtenerHistoriaPorSlugAsync(string slug)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(slug))
                    return ResultadoOperacion<HistoriaPublicaDetalleDto>.SetError("Slug requerido.");

                var now = DateTime.UtcNow;
                var slugNormalizado = slug.Trim().ToLower();

                var resultado = await QueryVendedoresConHistoriaPublica(now)
                    .Where(v => v.SlugTienda == slugNormalizado)
                    .Select(v => new
                    {
                        Vendedor = v,
                        TotalProductos = _context.Productos.Count(p =>
                            p.VendedorId == v.VendedorId &&
                            p.Estado == TipoEstadoProducto.Activo),
                        TotalVentas = _context.Subordenes.Count(s =>
                            s.VendedorId == v.VendedorId &&
                            s.Estado == TipoEstadoSuborden.Entregada)
                    })
                    .FirstOrDefaultAsync();

                if (resultado == null)
                    return ResultadoOperacion<HistoriaPublicaDetalleDto>.SetError("Historia no encontrada.");

                var categorias = await ObtenerCategoriasPrincipalesPorVendedorAsync(
                    new[] { resultado.Vendedor.VendedorId });

                return ResultadoOperacion<HistoriaPublicaDetalleDto>.SetExito(
                    resultado.Vendedor.ToHistoriaPublicaDetalleDto(
                        resultado.TotalProductos,
                        resultado.TotalVentas,
                        categorias.GetValueOrDefault(resultado.Vendedor.VendedorId)));
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<HistoriaPublicaDetalleDto>.SetError("Error: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // PEDIDOS DEL VENDEDOR
        // ─────────────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<PaginacionRespuestaDto<PedidoVendedorDto>>> ListarMisPedidosAsync(
            int usuarioId, TipoEstadoSuborden? estado, int pagina, int tamanioPagina)
        {
            try
            {
                pagina = Math.Max(1, pagina);
                tamanioPagina = Math.Clamp(tamanioPagina, 1, 50);

                var vendedor = await _context.Vendedores
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.UsuarioId == usuarioId);

                if (vendedor == null)
                    return ResultadoOperacion<PaginacionRespuestaDto<PedidoVendedorDto>>.SetError(
                        "No tienes un perfil de vendedor.");

                var query = _context.Subordenes
                    .Include(s => s.Orden).ThenInclude(o => o.Cliente).ThenInclude(c => c.Persona)
                    .Include(s => s.MetodoEnvio)
                    .Include(s => s.Envios)
                    .Include(s => s.Items)
                    .AsNoTracking()
                    .Where(s => s.VendedorId == vendedor.VendedorId);

                if (estado.HasValue)
                    query = query.Where(s => s.Estado == estado.Value);

                var total = await query.CountAsync();
                var subordenes = await query
                    .OrderByDescending(s => s.Orden.Fecha)
                    .Skip((pagina - 1) * tamanioPagina)
                    .Take(tamanioPagina)
                    .ToListAsync();

                var items = subordenes.Select(s => new PedidoVendedorDto
                {
                    SubordenId = s.SubordenId,
                    NumeroSuborden = s.NumeroSuborden,
                    NombreCliente = $"{s.Orden.Cliente.Persona.Nombres} {s.Orden.Cliente.Persona.ApellidoPaterno}".Trim(),
                    CorreoCliente = s.Orden.CorreoCliente,
                    Subtotal = s.Subtotal,
                    MontoEnvio = s.MontoEnvio,
                    MontoVendedor = s.MontoVendedor,
                    Estado = new EnumeracionDto((int)s.Estado, s.Estado.GetDescription()),
                    Fecha = s.Orden.Fecha,
                    TotalItems = s.Items.Sum(i => i.Cantidad),
                    MetodoEnvio = s.MetodoEnvio?.Nombre,
                    CodigoSeguimiento = s.Envios.OrderByDescending(e => e.EnvioId).FirstOrDefault()?.NumeroSeguimiento
                }).ToList();

                var resultado = new PaginacionRespuestaDto<PedidoVendedorDto>
                {
                    Items = items,
                    Pagina = pagina,
                    TamanioPagina = tamanioPagina,
                    TotalRegistros = total,
                    HayMas = pagina * tamanioPagina < total
                };

                return ResultadoOperacion<PaginacionRespuestaDto<PedidoVendedorDto>>.SetExito(resultado);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<PaginacionRespuestaDto<PedidoVendedorDto>>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<PedidoVendedorDetalleDto>> ObtenerMisPedidoDetalleAsync(
            int usuarioId, long subordenId)
        {
            try
            {
                var vendedor = await _context.Vendedores
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.UsuarioId == usuarioId);

                if (vendedor == null)
                    return ResultadoOperacion<PedidoVendedorDetalleDto>.SetError(
                        "No tienes un perfil de vendedor.");

                var suborden = await _context.Subordenes
                    .Include(s => s.Orden).ThenInclude(o => o.Cliente).ThenInclude(c => c.Persona)
                    .Include(s => s.MetodoEnvio)
                    .Include(s => s.Envios)
                    .Include(s => s.Items)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s =>
                        s.SubordenId == subordenId && s.VendedorId == vendedor.VendedorId);

                if (suborden == null)
                    return ResultadoOperacion<PedidoVendedorDetalleDto>.SetError("Pedido no encontrado.");

                var envio = suborden.Envios.OrderByDescending(e => e.EnvioId).FirstOrDefault();

                var direccion = DeserializarDireccion(suborden.Orden.DireccionEnvio);

                var detalle = new PedidoVendedorDetalleDto
                {
                    SubordenId = suborden.SubordenId,
                    NumeroSuborden = suborden.NumeroSuborden,
                    NumeroOrden = suborden.Orden.NumeroOrden,
                    NombreCliente =
                        $"{suborden.Orden.Cliente.Persona.Nombres} {suborden.Orden.Cliente.Persona.ApellidoPaterno}".Trim(),
                    CorreoCliente = suborden.Orden.CorreoCliente,
                    TelefonoCliente = suborden.Orden.TelefonoCliente ?? direccion.Telefono,
                    Subtotal = suborden.Subtotal,
                    MontoEnvio = suborden.MontoEnvio,
                    MontoComision = suborden.MontoComision,
                    MontoVendedor = suborden.MontoVendedor,
                    Estado = new EnumeracionDto((int)suborden.Estado, suborden.Estado.GetDescription()),
                    Fecha = suborden.Orden.Fecha,
                    FechaEnvio = suborden.FechaEnvio,
                    FechaEntrega = suborden.FechaEntrega,
                    TotalItems = suborden.Items.Sum(i => i.Cantidad),
                    MetodoEnvio = suborden.MetodoEnvio?.Nombre,
                    MetodoEnvioCodigo = suborden.MetodoEnvio?.Codigo,
                    MetodoEnvioDescripcion = suborden.MetodoEnvio?.Descripcion,
                    MetodoEnvioTiempoEstimadoDias = suborden.MetodoEnvio?.TiempoEstimadoDias,
                    CodigoSeguimiento = envio?.NumeroSeguimiento,
                    DireccionEnvio = direccion,
                    Envio = envio?.ToDto(),
                    Items = suborden.Items
                        .OrderBy(i => i.ItemOrdenId)
                        .Select(i => new ItemOrdenDto
                        {
                            ItemOrdenId = i.ItemOrdenId,
                            SubordenId = i.SubordenId,
                            VarianteId = i.VarianteId,
                            NombreProducto = i.NombreProducto,
                            NombreVariante = i.NombreVariante,
                            ImagenUrl = i.ImagenUrl,
                            PrecioUnitario = i.PrecioUnitario,
                            Cantidad = i.Cantidad,
                            TotalLinea = i.TotalLinea,
                            TipoProducto = new EnumeracionDto(
                                (int)i.TipoProducto, i.TipoProducto.GetDescription())
                        })
                        .ToList()
                };

                return ResultadoOperacion<PedidoVendedorDetalleDto>.SetExito(detalle);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<PedidoVendedorDetalleDto>.SetError("Error: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // ADMIN OVERVIEW
        // ─────────────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<PaginacionRespuestaDto<VendedorAdminListadoDto>>> ListarAdminAsync(
            string? busqueda, TipoEstadoVendedor? estado, bool? conSuscripcion, int pagina, int tamanioPagina)
        {
            try
            {
                pagina = Math.Max(1, pagina);
                tamanioPagina = Math.Clamp(tamanioPagina, 1, 50);

                var query = _context.Vendedores.AsNoTracking()
                    .Include(v => v.Usuario)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    var term = busqueda.Trim().ToLower();
                    query = query.Where(v =>
                        v.NombreTienda.ToLower().Contains(term) ||
                        v.SlugTienda.ToLower().Contains(term) ||
                        v.Usuario.Correo.ToLower().Contains(term));
                }

                if (estado.HasValue)
                    query = query.Where(v => v.Estado == estado.Value);

                var vendedores = await query.OrderByDescending(v => v.VendedorId).ToListAsync();
                var vendedorIds = vendedores.Select(v => v.VendedorId).ToList();

                var suscripciones = await _context.Suscripciones.AsNoTracking()
                    .Include(s => s.Plan)
                    .Where(s => vendedorIds.Contains(s.VendedorId))
                    .GroupBy(s => s.VendedorId)
                    .Select(g => g.OrderByDescending(s => s.SuscripcionId).First())
                    .ToListAsync();

                var susPorVendedor = suscripciones.ToDictionary(s => s.VendedorId);

                if (conSuscripcion == true)
                    vendedores = vendedores.Where(v => susPorVendedor.ContainsKey(v.VendedorId)).ToList();
                else if (conSuscripcion == false)
                    vendedores = vendedores.Where(v => !susPorVendedor.ContainsKey(v.VendedorId)).ToList();

                var total = vendedores.Count;
                var page = vendedores.Skip((pagina - 1) * tamanioPagina).Take(tamanioPagina).ToList();

                var items = new List<VendedorAdminListadoDto>();
                foreach (var v in page)
                {
                    susPorVendedor.TryGetValue(v.VendedorId, out var sus);
                    var totalProductos = await _context.Productos.CountAsync(p => p.VendedorId == v.VendedorId);
                    items.Add(new VendedorAdminListadoDto
                    {
                        VendedorId = v.VendedorId,
                        NombreTienda = v.NombreTienda,
                        SlugTienda = v.SlugTienda,
                        CorreoUsuario = v.Usuario.Correo,
                        Estado = new EnumeracionDto((int)v.Estado, v.Estado.ToString()),
                        PlanActual = sus?.Plan?.Nombre,
                        EstadoSuscripcion = sus != null
                            ? new EnumeracionDto((int)sus.Estado, sus.Estado.ToString())
                            : null,
                        TotalProductos = totalProductos,
                        TotalVentas = v.TotalVentas,
                        CalificacionPromedio = v.CalificacionPromedio,
                        FechaRegistro = v.Usuario?.FechaAlta ?? DateTime.UtcNow
                    });
                }

                return ResultadoOperacion<PaginacionRespuestaDto<VendedorAdminListadoDto>>.SetExito(
                    new PaginacionRespuestaDto<VendedorAdminListadoDto>
                    {
                        Items = items, Pagina = pagina, TamanioPagina = tamanioPagina,
                        TotalRegistros = total, HayMas = pagina * tamanioPagina < total
                    });
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<PaginacionRespuestaDto<VendedorAdminListadoDto>>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<VendedorAdminDetalleDto>> ObtenerAdminDetalleAsync(int vendedorId)
        {
            try
            {
                var v = await _context.Vendedores.AsNoTracking()
                    .Include(x => x.Usuario)
                    .FirstOrDefaultAsync(x => x.VendedorId == vendedorId);
                if (v == null) return ResultadoOperacion<VendedorAdminDetalleDto>.SetError("Vendedor no encontrado.");

                var baseDto = (await ListarAdminAsync(v.NombreTienda, null, null, 1, 50)).Datos?.Items
                    .FirstOrDefault(x => x.VendedorId == vendedorId);

                var montoVendido = await _context.Subordenes
                    .Where(s => s.VendedorId == vendedorId)
                    .SumAsync(s => (decimal?)s.Subtotal) ?? 0;
                var comision = await _context.Subordenes
                    .Where(s => s.VendedorId == vendedorId)
                    .SumAsync(s => (decimal?)s.MontoComision) ?? 0;
                var reclamos = await _context.Reclamos
                    .CountAsync(r => r.Suborden.VendedorId == vendedorId &&
                        (r.Estado == TipoEstadoReclamo.Abierto || r.Estado == TipoEstadoReclamo.EnRevision));
                var ultimaVenta = await _context.Subordenes
                    .Where(s => s.VendedorId == vendedorId)
                    .OrderByDescending(s => s.SubordenId)
                    .Select(s => (DateTime?)s.Orden.Fecha)
                    .FirstOrDefaultAsync();

                return ResultadoOperacion<VendedorAdminDetalleDto>.SetExito(new VendedorAdminDetalleDto
                {
                    VendedorId = v.VendedorId,
                    NombreTienda = v.NombreTienda,
                    SlugTienda = v.SlugTienda,
                    CorreoUsuario = v.Usuario.Correo,
                    Estado = new EnumeracionDto((int)v.Estado, v.Estado.ToString()),
                    PlanActual = baseDto?.PlanActual,
                    EstadoSuscripcion = baseDto?.EstadoSuscripcion,
                    TotalProductos = baseDto?.TotalProductos ?? 0,
                    TotalVentas = v.TotalVentas,
                    CalificacionPromedio = v.CalificacionPromedio,
                    FechaRegistro = v.Usuario.FechaAlta,
                    LogoUrl = v.LogoUrl,
                    BannerUrl = v.BannerUrl,
                    Biografia = v.Biografia,
                    VendePatrones = v.VendePatrones,
                    MontoVendidoTotal = montoVendido,
                    ComisionGenerada = comision,
                    ReclamosAbiertos = reclamos,
                    FechaUltimaVenta = ultimaVenta
                });
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<VendedorAdminDetalleDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> SuspenderAdminAsync(int vendedorId, SuspenderVendedorDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Motivo) || dto.Motivo.Trim().Length < 20)
                    return ResultadoOperacion<bool>.SetError("El motivo debe tener al menos 20 caracteres.");

                var v = await _context.Vendedores
                    .Include(x => x.Usuario)
                    .FirstOrDefaultAsync(x => x.VendedorId == vendedorId);
                if (v == null) return ResultadoOperacion<bool>.SetError("Vendedor no encontrado.");

                v.Estado = TipoEstadoVendedor.Suspendido;
                var productos = await _context.Productos
                    .Where(p => p.VendedorId == vendedorId && p.Estado == TipoEstadoProducto.Activo)
                    .ToListAsync();
                foreach (var p in productos) p.Estado = TipoEstadoProducto.Pausado;

                await _context.SaveChangesAsync();

                await _notificacionServicio.CrearAsync(
                    v.UsuarioId,
                    TipoNotificacion.VendedorSuspendido,
                    "Tu tienda fue suspendida",
                    $"Motivo: {dto.Motivo.Trim()}");

                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> ReactivarAdminAsync(int vendedorId)
        {
            try
            {
                var v = await _context.Vendedores.FirstOrDefaultAsync(x => x.VendedorId == vendedorId);
                if (v == null) return ResultadoOperacion<bool>.SetError("Vendedor no encontrado.");
                v.Estado = TipoEstadoVendedor.Activo;
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.SetError("Error: " + ex.Message);
            }
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

        private async Task<VendedorPerfilDto> ConstruirPerfilConMetricasAsync(Vendedor vendedor)
        {
            var dto = vendedor.ToPerfilDto();
            dto.TotalVentas = await VendedorMetricasHelper.ContarVentasEntregadasAsync(
                _context, vendedor.VendedorId);
            dto.TotalProductos = await VendedorMetricasHelper.ContarProductosActivosAsync(
                _context, vendedor.VendedorId);
            dto.CalificacionPromedio = await VendedorMetricasHelper.ObtenerCalificacionPromedioAsync(
                _context, vendedor.VendedorId);
            return dto;
        }

        private IQueryable<Vendedor> QueryVendedoresConHistoriaPublica(DateTime now) =>
            _context.Vendedores
                .AsNoTracking()
                .Where(v => v.Estado == TipoEstadoVendedor.Activo
                         && v.Biografia != null
                         && v.Biografia.Trim() != string.Empty)
                .ConPlanActivo(_context, now);

        private async Task<Dictionary<int, string?>> ObtenerCategoriasPrincipalesPorVendedorAsync(
            IReadOnlyCollection<int> vendedorIds)
        {
            if (vendedorIds.Count == 0)
                return new Dictionary<int, string?>();

            var conteos = await _context.Productos
                .AsNoTracking()
                .Where(p => vendedorIds.Contains(p.VendedorId) && p.Estado == TipoEstadoProducto.Activo)
                .GroupBy(p => new { p.VendedorId, CategoriaNombre = p.Categoria.Nombre })
                .Select(g => new
                {
                    g.Key.VendedorId,
                    g.Key.CategoriaNombre,
                    Total = g.Count()
                })
                .ToListAsync();

            return conteos
                .GroupBy(x => x.VendedorId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.Total).Select(x => x.CategoriaNombre).FirstOrDefault());
        }
    }
}
