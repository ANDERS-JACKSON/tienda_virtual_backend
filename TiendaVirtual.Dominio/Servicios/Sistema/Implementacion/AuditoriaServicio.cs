using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;

namespace TiendaVirtual.Dominio.Servicios.Sistema.Implementacion
{
    public class AuditoriaServicio : IAuditoriaServicio
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AuditoriaServicio> _logger;

        public AuditoriaServicio(IServiceScopeFactory scopeFactory, ILogger<AuditoriaServicio> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task RegistrarAsync(string accion, Guid? usuarioId, string? direccionIp, string? detalle)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<TiendaVirtualDbContext>();
                context.EventosAuditoria.Add(new EventoAuditoria
                {
                    Fecha = DateTime.UtcNow,
                    Accion = accion.Length > 80 ? accion[..80] : accion,
                    UsuarioId = usuarioId,
                    DireccionIp = Truncar(direccionIp, 45),
                    Detalle = Truncar(detalle, 500)
                });
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo persistir auditoría {Accion}.", accion);
            }
        }

        private static string? Truncar(string? valor, int max) =>
            string.IsNullOrEmpty(valor) ? valor : valor.Length <= max ? valor : valor[..max];
    }
}
