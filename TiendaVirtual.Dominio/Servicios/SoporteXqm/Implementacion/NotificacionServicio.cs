using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaVirtual.Comun.Enumeracion;
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
            object? datos = null,
            PlantillaCorreo? plantillaEmail = null,
            Dictionary<string, string>? placeholdersEmail = null)
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

            if (!plantillaEmail.HasValue) return;

            var datosUsuario = await _context.Usuarios
                .Where(u => u.UsuarioId == usuarioId)
                .Select(u => new
                {
                    u.Correo,
                    Nombre = u.Persona.Nombres,
                    Apellido = u.Persona.ApellidoPaterno ?? string.Empty
                })
                .FirstOrDefaultAsync();

            if (datosUsuario == null || string.IsNullOrWhiteSpace(datosUsuario.Correo))
            {
                _logger.LogWarning(
                    "Usuario {UsuarioId} sin correo. No se envía email de tipo {Tipo}.",
                    usuarioId, tipo);
                return;
            }

            var nombreCompleto = $"{datosUsuario.Nombre} {datosUsuario.Apellido}".Trim();
            var placeholders = placeholdersEmail ?? new Dictionary<string, string>();

            _ = Task.Run(async () =>
            {
                try
                {
                    await _email.EnviarAsync(
                        datosUsuario.Correo, nombreCompleto,
                        plantillaEmail.Value, placeholders);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error enviando email de notificación tipo {Tipo} (plantilla {Plantilla}) a usuario {UsuarioId}",
                        tipo, plantillaEmail.Value, usuarioId);
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
                    .AsNoTracking()
                    .CountAsync(n => n.UsuarioId == usuarioId && !n.Leida);

                return ResultadoOperacion<int>.SetExito(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error contando notificaciones no leídas del usuario {UsuarioId}",
                    usuarioId);
                return ResultadoOperacion<int>.SetExito(0);
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
    }
}
