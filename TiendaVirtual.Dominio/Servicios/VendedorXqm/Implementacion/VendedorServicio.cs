using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Extensiones.VendedorXqm;
using TiendaVirtual.Dominio.Servicios.SoporteXqm;
using TiendaVirtual.Dominio.Servicios.SuscripcionXqm.Implementacion;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;

namespace TiendaVirtual.Dominio.Servicios.VendedorXqm.Implementacion
{
    public class VendedorServicio : IVendedorServicio
    {
        protected readonly TiendaVirtualDbContext _context;
        private readonly INotificacionServicio _notificacionServicio;

        public VendedorServicio(TiendaVirtualDbContext context, INotificacionServicio notificacionServicio)
        {
            _context = context;
            _notificacionServicio = notificacionServicio;
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

                return ResultadoOperacion<VendedorPerfilDto>.SetExito(vendedor.ToPerfilDto());
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<VendedorPerfilDto>.SetError("Error al obtener el perfil: " + ex.Message);
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
                return ResultadoOperacion<VendedorPerfilDto>.SetExito(vendedor.ToPerfilDto());
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
                return ResultadoOperacion<VendedorPerfilDto>.SetExito(vendedor.ToPerfilDto());
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
                    "Ya puedes publicar productos y vender en Artesanías.");

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
                    $"Motivo: {dto.MotivoRechazo}. Puedes corregir y reenviar la solicitud.");

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
                    .OrderByDescending(v => v.TotalVentas)
                    .ThenByDescending(v => v.CalificacionPromedio)
                    .Skip((pagina - 1) * tamanioPagina)
                    .Take(tamanioPagina)
                    .Select(v => new
                    {
                        Vendedor = v,
                        TotalProductos = _context.Productos.Count(p =>
                            p.VendedorId == v.VendedorId &&
                            p.Estado == TipoEstadoProducto.Activo)
                    })
                    .ToListAsync();

                var resultado = new PaginacionRespuestaDto<TiendaPublicaDto>
                {
                    Items = resultados.Select(r => r.Vendedor.ToTiendaPublicaDto(r.TotalProductos)).ToList(),
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
                            p.Estado == TipoEstadoProducto.Activo)
                    })
                    .FirstOrDefaultAsync();

                if (resultado == null)
                    return ResultadoOperacion<TiendaPublicaDto>.SetError("Tienda no encontrada.");

                return ResultadoOperacion<TiendaPublicaDto>.SetExito(
                    resultado.Vendedor.ToTiendaPublicaDto(resultado.TotalProductos));
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<TiendaPublicaDto>.SetError("Error: " + ex.Message);
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
                    Estado = new EnumeracionDto((int)s.Estado, s.Estado.ToString()),
                    Fecha = s.Orden.Fecha,
                    TotalItems = s.Items.Sum(i => i.Cantidad)
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
    }
}
