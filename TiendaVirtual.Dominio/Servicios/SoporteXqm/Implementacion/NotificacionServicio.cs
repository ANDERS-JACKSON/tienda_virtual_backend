using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaVirtual.Dominio.Modelo.SoporteXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.SoporteXqm;

namespace TiendaVirtual.Dominio.Servicios.SoporteXqm.Implementacion
{
    public class NotificacionServicio : INotificacionServicio
    {
        private readonly TiendaVirtualDbContext _context;
        private readonly IEmailServicio _email;
        private readonly ILogger<NotificacionServicio> _logger;

        public NotificacionServicio(
            TiendaVirtualDbContext context,
            IEmailServicio email,
            ILogger<NotificacionServicio> logger)
        {
            _context = context;
            _email = email;
            _logger = logger;
        }

        public async Task CrearAsync(int usuarioId, string tipo, string titulo, string cuerpo,
            object? datos = null, bool enviarEmail = true)
        {
            var notificacion = new Notificacion
            {
                UsuarioId = usuarioId,
                Tipo = tipo,
                Titulo = titulo,
                Cuerpo = cuerpo,
                Datos = datos != null ? JsonSerializer.Serialize(datos) : null,
                Leida = false,
                Fecha = DateTime.UtcNow
            };
            _context.Notificaciones.Add(notificacion);
            await _context.SaveChangesAsync();

            if (!enviarEmail) return;

            var datosUsuario = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.UsuarioId == usuarioId)
                .Select(u => new
                {
                    u.Correo,
                    Nombre = u.Persona.Nombres,
                    Apellido = u.Persona.ApellidoPaterno ?? string.Empty
                })
                .FirstOrDefaultAsync();

            if (datosUsuario == null || string.IsNullOrWhiteSpace(datosUsuario.Correo))
                return;

            var nombreCompleto = $"{datosUsuario.Nombre} {datosUsuario.Apellido}".Trim();
            var html = ConstruirHtml(titulo, cuerpo, nombreCompleto);

            _ = Task.Run(async () =>
            {
                try
                {
                    await _email.EnviarAsync(datosUsuario.Correo, nombreCompleto, titulo, html);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error en envío de email para notificación tipo {Tipo} a usuario {UsuarioId}",
                        tipo, usuarioId);
                }
            });
        }

        public async Task<ResultadoOperacion<PaginacionRespuestaDto<NotificacionDto>>> ListarMisAsync(
            int usuarioId, int pagina, int tamanioPagina)
        {
            try
            {
                pagina = Math.Max(1, pagina);
                tamanioPagina = Math.Clamp(tamanioPagina, 1, 50);

                var query = _context.Notificaciones.AsNoTracking()
                    .Where(n => n.UsuarioId == usuarioId)
                    .OrderByDescending(n => n.NotificacionId);

                var total = await query.CountAsync();
                var items = await query
                    .Skip((pagina - 1) * tamanioPagina)
                    .Take(tamanioPagina)
                    .Select(n => new NotificacionDto
                    {
                        NotificacionId = n.NotificacionId,
                        Tipo = n.Tipo,
                        Titulo = n.Titulo,
                        Cuerpo = n.Cuerpo,
                        Datos = n.Datos,
                        Leida = n.Leida,
                        Fecha = n.Fecha
                    })
                    .ToListAsync();

                return ResultadoOperacion<PaginacionRespuestaDto<NotificacionDto>>.SetExito(
                    new PaginacionRespuestaDto<NotificacionDto>
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
                return ResultadoOperacion<PaginacionRespuestaDto<NotificacionDto>>.SetError(
                    "Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<int>> ContarNoLeidasAsync(int usuarioId)
        {
            try
            {
                var count = await _context.Notificaciones
                    .CountAsync(n => n.UsuarioId == usuarioId && !n.Leida);
                return ResultadoOperacion<int>.SetExito(count);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<int>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> MarcarLeidaAsync(int usuarioId, long notificacionId)
        {
            try
            {
                var n = await _context.Notificaciones.FirstOrDefaultAsync(
                    x => x.NotificacionId == notificacionId && x.UsuarioId == usuarioId);
                if (n == null)
                    return ResultadoOperacion<bool>.SetError("Notificación no encontrada.");
                if (n.Leida)
                    return ResultadoOperacion<bool>.SetExito(true);

                n.Leida = true;
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<int>> MarcarTodasLeidasAsync(int usuarioId)
        {
            try
            {
                var noLeidas = await _context.Notificaciones
                    .Where(n => n.UsuarioId == usuarioId && !n.Leida)
                    .ToListAsync();
                foreach (var n in noLeidas)
                    n.Leida = true;
                await _context.SaveChangesAsync();
                return ResultadoOperacion<int>.SetExito(noLeidas.Count);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<int>.SetError("Error: " + ex.Message);
            }
        }

        private static string ConstruirHtml(string titulo, string cuerpo, string nombre)
        {
            return $@"<!DOCTYPE html>
<html><body style=""margin:0;padding:0;background:#f5f7fb;font-family:Arial,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#f5f7fb;padding:24px 0;"">
    <tr><td align=""center"">
      <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background:white;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.06);"">
        <tr><td style=""background:#0a2540;padding:24px;text-align:center;"">
          <h1 style=""color:white;margin:0;font-size:24px;letter-spacing:2px;"">ARTESANÍAS</h1>
        </td></tr>
        <tr><td style=""padding:32px;"">
          <h2 style=""color:#0a2540;font-size:20px;margin:0 0 16px 0;"">Hola, {WebUtility.HtmlEncode(nombre)}</h2>
          <h3 style=""color:#0a2540;font-size:18px;margin:0 0 12px 0;"">{WebUtility.HtmlEncode(titulo)}</h3>
          <p style=""color:#4b5563;line-height:1.6;margin:0 0 24px 0;font-size:15px;"">{WebUtility.HtmlEncode(cuerpo)}</p>
          <p style=""color:#6b7280;font-size:13px;margin-top:24px;"">Ingresa a tu cuenta en la plataforma para ver más detalles.</p>
        </td></tr>
        <tr><td style=""background:#f9fafb;padding:16px;text-align:center;border-top:1px solid #e5e7eb;"">
          <p style=""color:#9ca3af;font-size:12px;margin:0;"">Este es un correo automático del sistema Artesanías.</p>
        </td></tr>
      </table>
    </td></tr>
  </table>
</body></html>";
        }
    }
}
