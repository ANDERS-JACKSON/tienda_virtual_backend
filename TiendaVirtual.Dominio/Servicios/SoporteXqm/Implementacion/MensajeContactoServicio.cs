using System.Net.Mail;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Extensiones;
using TiendaVirtual.Dominio.Modelo.SoporteXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.SoporteXqm;

namespace TiendaVirtual.Dominio.Servicios.SoporteXqm.Implementacion
{
    public class MensajeContactoServicio : IMensajeContactoServicio
    {
        private readonly TiendaVirtualDbContext _context;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MensajeContactoServicio> _logger;

        public MensajeContactoServicio(
            TiendaVirtualDbContext context,
            IServiceScopeFactory scopeFactory,
            ILogger<MensajeContactoServicio> logger)
        {
            _context = context;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<ResultadoOperacion<long>> CrearAsync(
            CrearMensajeContactoDto dto, int? usuarioIdSiLogueado)
        {
            try
            {
                if (dto == null)
                    return ResultadoOperacion<long>.SetError("Datos inválidos.");

                if (!string.IsNullOrWhiteSpace(dto.Sitio))
                    return ResultadoOperacion<long>.SetExito(0L);

                var nombre = dto.Nombre.Normalizar();
                var correo = dto.Correo.Trim().ToLowerInvariant();
                var asunto = dto.Asunto.Normalizar();
                var mensaje = dto.Mensaje.Normalizar();

                var validacion = ValidarCampos(nombre, correo, asunto, mensaje);
                if (validacion != null)
                    return ResultadoOperacion<long>.SetError(validacion);

                var haceUnaHora = DateTime.UtcNow.AddHours(-1);
                var enviosRecientes = await _context.MensajesContacto
                    .CountAsync(m => m.Correo == correo && m.FechaMensaje >= haceUnaHora);
                if (enviosRecientes >= 3)
                    return ResultadoOperacion<long>.SetError(
                        "Has enviado demasiados mensajes. Intenta de nuevo más tarde.");

                var haceCincoMin = DateTime.UtcNow.AddMinutes(-5);
                var duplicado = await _context.MensajesContacto
                    .AsNoTracking()
                    .Where(m =>
                        m.Correo == correo &&
                        m.Asunto == asunto &&
                        m.Mensaje == mensaje &&
                        m.FechaMensaje >= haceCincoMin)
                    .Select(m => m.MensajeContactoId)
                    .FirstOrDefaultAsync();
                if (duplicado > 0)
                    return ResultadoOperacion<long>.SetExito(duplicado);

                var entidad = new MensajeContacto
                {
                    UsuarioId = usuarioIdSiLogueado,
                    Nombre = nombre,
                    Correo = correo,
                    Asunto = asunto,
                    Mensaje = mensaje,
                    Estado = TipoEstadoContacto.Nuevo,
                    FechaMensaje = DateTime.UtcNow
                };

                _context.MensajesContacto.Add(entidad);
                await _context.SaveChangesAsync();

                var mensajeId = entidad.MensajeContactoId;
                _ = Task.Run(() => NotificarAdminsAsync(mensajeId));

                return ResultadoOperacion<long>.SetExito(mensajeId);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<long>.SetError("Error al enviar el mensaje: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<MensajeContactoDetalleDto>> ObtenerDetalleAsync(long id)
        {
            try
            {
                var dto = await CargarDetalleAsync(id);

                if (dto == null)
                    return ResultadoOperacion<MensajeContactoDetalleDto>.SetError("Mensaje no encontrado.");

                return ResultadoOperacion<MensajeContactoDetalleDto>.SetExito(dto);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<MensajeContactoDetalleDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<PaginacionRespuestaDto<MensajeContactoListadoDto>>> ListarAsync(
            int pagina, int tamanio, int? estado, string? busqueda)
        {
            try
            {
                pagina = Math.Max(1, pagina);
                tamanio = Math.Clamp(tamanio, 1, 100);

                var query = _context.MensajesContacto.AsNoTracking();

                if (estado.HasValue)
                    query = query.Where(m => (int)m.Estado == estado.Value);

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    var term = busqueda.Trim().ToUpper();
                    query = query.Where(m =>
                        EF.Functions.ILike(m.Nombre, $"%{term}%") ||
                        EF.Functions.ILike(m.Correo, $"%{term}%") ||
                        EF.Functions.ILike(m.Asunto, $"%{term}%"));
                }

                query = query.OrderByDescending(m => m.FechaMensaje);

                var total = await query.CountAsync();
                var rows = await query
                    .Skip((pagina - 1) * tamanio)
                    .Take(tamanio)
                    .ToListAsync();

                var items = rows.Select(m => new MensajeContactoListadoDto
                {
                    Id = m.MensajeContactoId,
                    Nombre = m.Nombre,
                    Correo = m.Correo,
                    Asunto = m.Asunto,
                    Estado = MapEstadoDto(m.Estado),
                    FechaMensaje = m.FechaMensaje,
                    FueRespondido = m.Estado == TipoEstadoContacto.Respondido
                }).ToList();

                return ResultadoOperacion<PaginacionRespuestaDto<MensajeContactoListadoDto>>.SetExito(
                    new PaginacionRespuestaDto<MensajeContactoListadoDto>
                    {
                        Items = items,
                        Pagina = pagina,
                        TamanioPagina = tamanio,
                        TotalRegistros = total,
                        HayMas = pagina * tamanio < total
                    });
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<PaginacionRespuestaDto<MensajeContactoListadoDto>>.SetError(
                    "Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> ResponderAsync(
            long id, int adminId, ResponderMensajeContactoDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Respuesta))
                    return ResultadoOperacion<bool>.SetError("La respuesta es obligatoria.");
                if (dto.Respuesta.Length > 1000)
                    return ResultadoOperacion<bool>.SetError("La respuesta no puede superar 1000 caracteres.");

                var respuesta = dto.Respuesta.Normalizar();

                using var trx = await _context.Database.BeginTransactionAsync();

                var mensaje = await _context.MensajesContacto
                    .FirstOrDefaultAsync(m => m.MensajeContactoId == id);

                if (mensaje == null)
                    return ResultadoOperacion<bool>.SetError("Mensaje no encontrado.");

                if (mensaje.Estado is TipoEstadoContacto.Archivado or TipoEstadoContacto.Spam)
                    return ResultadoOperacion<bool>.SetError(
                        "No se puede responder un mensaje archivado o marcado como spam.");

                mensaje.Respuesta = respuesta;
                mensaje.RespondidoPor = adminId;
                mensaje.FechaRespuesta = DateTime.UtcNow;
                mensaje.Estado = TipoEstadoContacto.Respondido;

                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                var snapshot = new MensajeContactoSnapshot
                {
                    Id = mensaje.MensajeContactoId,
                    Nombre = mensaje.Nombre,
                    Correo = mensaje.Correo,
                    Asunto = mensaje.Asunto,
                    Mensaje = mensaje.Mensaje,
                    Respuesta = respuesta,
                    UsuarioId = mensaje.UsuarioId
                };

                _ = Task.Run(() => EnviarRespuestaAsync(snapshot));

                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.SetError("Error al responder: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> CambiarEstadoAsync(
            long id, int adminId, TipoEstadoContacto nuevoEstado)
        {
            try
            {
                var mensaje = await _context.MensajesContacto
                    .FirstOrDefaultAsync(m => m.MensajeContactoId == id);

                if (mensaje == null)
                    return ResultadoOperacion<bool>.SetError("Mensaje no encontrado.");

                if (!EsTransicionValida(mensaje.Estado, nuevoEstado))
                    return ResultadoOperacion<bool>.SetError("Transición de estado no permitida.");

                mensaje.Estado = nuevoEstado;
                await _context.SaveChangesAsync();

                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<ContadorMensajesContactoDto>> ContarNoLeidosAsync()
        {
            try
            {
                var total = await _context.MensajesContacto
                    .CountAsync(m => m.Estado == TipoEstadoContacto.Nuevo);

                return ResultadoOperacion<ContadorMensajesContactoDto>.SetExito(
                    new ContadorMensajesContactoDto { TotalNoLeidos = total });
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<ContadorMensajesContactoDto>.SetError("Error: " + ex.Message);
            }
        }

        private async Task NotificarAdminsAsync(long mensajeId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<TiendaVirtualDbContext>();
                var notif = scope.ServiceProvider.GetRequiredService<INotificacionServicio>();

                var mensaje = await context.MensajesContacto
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.MensajeContactoId == mensajeId);
                if (mensaje == null) return;

                var admins = await context.Usuarios
                    .AsNoTracking()
                    .Include(u => u.Persona)
                    .Include(u => u.UsuarioRoles)
                    .ThenInclude(ur => ur.Rol)
                    .Where(u => u.UsuarioRoles.Any(ur => ur.Rol.Nombre == "ADMIN"))
                    .ToListAsync();

                var fecha = mensaje.FechaMensaje.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

                foreach (var admin in admins)
                {
                    var nombreAdmin = admin.Persona != null
                        ? $"{admin.Persona.Nombres} {admin.Persona.ApellidoPaterno}".Trim()
                        : admin.Correo;

                    var placeholders = new Dictionary<string, string>
                    {
                        ["destinatario"] = nombreAdmin,
                        ["nombre"] = mensaje.Nombre,
                        ["correo"] = mensaje.Correo,
                        ["asunto"] = mensaje.Asunto,
                        ["mensaje"] = mensaje.Mensaje,
                        ["fecha"] = fecha
                    };

                    try
                    {
                        await notif.CrearAsync(
                            admin.UsuarioId,
                            TipoNotificacion.MensajeContactoNuevo,
                            "Nuevo mensaje de contacto",
                            $"{mensaje.Nombre} ({mensaje.Correo}): {mensaje.Asunto}",
                            new
                            {
                                mensajeContactoId = mensaje.MensajeContactoId,
                                correoRemitente = mensaje.Correo,
                                asunto = mensaje.Asunto
                            },
                            PlantillaCorreo.NuevoMensajeContacto,
                            placeholders);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error notificando admin {AdminId} por mensaje contacto {MensajeId}",
                            admin.UsuarioId, mensajeId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en NotificarAdminsAsync para mensaje {MensajeId}", mensajeId);
            }
        }

        private async Task EnviarRespuestaAsync(MensajeContactoSnapshot snapshot)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var email = scope.ServiceProvider.GetRequiredService<IEmailServicio>();
                var notif = scope.ServiceProvider.GetRequiredService<INotificacionServicio>();

                var asunto = $"Respuesta a tu consulta: {snapshot.Asunto}";
                var cuerpo = ConstruirHtmlRespuesta(snapshot.Nombre, snapshot.Asunto, snapshot.Mensaje, snapshot.Respuesta);

                await email.EnviarHtmlAsync(snapshot.Correo, snapshot.Nombre, asunto, cuerpo);

                if (snapshot.UsuarioId.HasValue)
                {
                    await notif.CrearAsync(
                        snapshot.UsuarioId.Value,
                        TipoNotificacion.MensajeContactoRespondido,
                        "Respuesta a tu mensaje de contacto",
                        $"Respondimos tu consulta \"{snapshot.Asunto}\".",
                        new { mensajeContactoId = snapshot.Id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando respuesta de contacto {MensajeId}", snapshot.Id);
            }
        }

        private async Task<MensajeContactoDetalleDto?> CargarDetalleAsync(long id)
        {
            var m = await _context.MensajesContacto
                .AsNoTracking()
                .Include(x => x.RespondidoPorUsuario!)
                .ThenInclude(u => u.Persona)
                .FirstOrDefaultAsync(x => x.MensajeContactoId == id);

            if (m == null) return null;

            return new MensajeContactoDetalleDto
            {
                Id = m.MensajeContactoId,
                UsuarioId = m.UsuarioId,
                Nombre = m.Nombre,
                Correo = m.Correo,
                Asunto = m.Asunto,
                Mensaje = m.Mensaje,
                Estado = MapEstadoDto(m.Estado),
                Respuesta = m.Respuesta,
                RespondidoPor = m.RespondidoPor,
                NombreRespondedor = m.RespondidoPorUsuario?.Persona != null
                    ? $"{m.RespondidoPorUsuario.Persona.Nombres} {m.RespondidoPorUsuario.Persona.ApellidoPaterno}".Trim()
                    : null,
                FechaRespuesta = m.FechaRespuesta,
                FechaMensaje = m.FechaMensaje
            };
        }

        private static EnumeracionDto MapEstadoDto(TipoEstadoContacto estado) =>
            new() { Id = (int)estado, Nombre = estado.GetDescription() };

        private static string? ValidarCampos(string nombre, string correo, string asunto, string mensaje)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return "El nombre es obligatorio.";
            if (nombre.Length > 150)
                return "El nombre no puede superar 150 caracteres.";
            if (string.IsNullOrWhiteSpace(correo))
                return "El correo es obligatorio.";
            if (!EsCorreoValido(correo))
                return "El correo no tiene un formato válido.";
            if (string.IsNullOrWhiteSpace(asunto))
                return "El asunto es obligatorio.";
            if (asunto.Length > 200)
                return "El asunto no puede superar 200 caracteres.";
            if (string.IsNullOrWhiteSpace(mensaje))
                return "El mensaje es obligatorio.";
            if (mensaje.Length > 1000)
                return "El mensaje no puede superar 1000 caracteres.";
            return null;
        }

        private static bool EsCorreoValido(string correo)
        {
            try
            {
                _ = new MailAddress(correo);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool EsTransicionValida(TipoEstadoContacto actual, TipoEstadoContacto nuevo)
        {
            if (actual == nuevo) return true;

            return actual switch
            {
                TipoEstadoContacto.Nuevo => nuevo is TipoEstadoContacto.Leido
                    or TipoEstadoContacto.Spam
                    or TipoEstadoContacto.Archivado,
                TipoEstadoContacto.Leido => nuevo is TipoEstadoContacto.Archivado or TipoEstadoContacto.Spam,
                TipoEstadoContacto.Respondido => nuevo == TipoEstadoContacto.Archivado,
                TipoEstadoContacto.Archivado => nuevo == TipoEstadoContacto.Leido,
                TipoEstadoContacto.Spam => false,
                _ => false
            };
        }

        private static string ConstruirHtmlRespuesta(
            string nombre, string asuntoOriginal, string mensajeOriginal, string respuesta)
        {
            var sb = new StringBuilder();
            sb.Append("""
                <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;">
                  <div style="background:#1e3a8a;color:#fff;padding:16px 20px;border-radius:8px 8px 0 0;">
                    <strong>Artesanías Perú</strong>
                  </div>
                  <div style="padding:20px;border:1px solid #e5e7eb;border-top:none;">
                """);
            sb.Append($"<p>Hola <strong>{System.Net.WebUtility.HtmlEncode(nombre)}</strong>,</p>");
            sb.Append("<p>Hemos respondido tu consulta enviada desde nuestro formulario de contacto.</p>");
            sb.Append($"<p><strong>Asunto:</strong> {System.Net.WebUtility.HtmlEncode(asuntoOriginal)}</p>");
            sb.Append("<p><strong>Tu mensaje:</strong></p>");
            sb.Append($"<blockquote style=\"border-left:4px solid #1e3a8a;margin:0;padding:8px 16px;background:#f8fafc;\">{System.Net.WebUtility.HtmlEncode(mensajeOriginal)}</blockquote>");
            sb.Append("<p><strong>Nuestra respuesta:</strong></p>");
            sb.Append($"<blockquote style=\"border-left:4px solid #16a34a;margin:0;padding:8px 16px;background:#f0fdf4;\">{System.Net.WebUtility.HtmlEncode(respuesta)}</blockquote>");
            sb.Append("""
                  </div>
                  <div style="background:#f3f4f6;padding:12px 20px;font-size:12px;color:#6b7280;border-radius:0 0 8px 8px;border:1px solid #e5e7eb;border-top:none;">
                    Este correo es una respuesta a tu mensaje en artesanias.pe
                  </div>
                </div>
                """);
            return sb.ToString();
        }

        private sealed class MensajeContactoSnapshot
        {
            public long Id { get; init; }
            public string Nombre { get; init; } = null!;
            public string Correo { get; init; } = null!;
            public string Asunto { get; init; } = null!;
            public string Mensaje { get; init; } = null!;
            public string Respuesta { get; init; } = null!;
            public int? UsuarioId { get; init; }
        }
    }
}
