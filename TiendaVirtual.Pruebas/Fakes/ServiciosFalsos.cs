using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Servicios.SeguridadXqm;
using TiendaVirtual.Dominio.Servicios.Sistema;
using TiendaVirtual.Dominio.Servicios.SoporteXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.SoporteXqm;

namespace TiendaVirtual.Pruebas.Fakes
{
    /// <summary>Notificaciones: no hace nada, sólo para satisfacer la dependencia.</summary>
    public class NotificacionServicioFalso : INotificacionServicio
    {
        public Task CrearAsync(Guid usuarioId, string tipo, string titulo, string cuerpo,
            object? datos = null, PlantillaCorreo? plantillaEmail = null,
            Dictionary<string, string>? placeholdersEmail = null) => Task.CompletedTask;

        public Task<ResultadoOperacion<PaginacionRespuestaDto<NotificacionDto>>> ListarMisAsync(
            Guid usuarioId, int pagina, int tamanioPagina, bool soloNoLeidas = false) =>
            Task.FromResult(ResultadoOperacion<PaginacionRespuestaDto<NotificacionDto>>.SetExito(
                new PaginacionRespuestaDto<NotificacionDto>()));

        public Task<ResultadoOperacion<int>> ContarNoLeidasAsync(Guid usuarioId) =>
            Task.FromResult(ResultadoOperacion<int>.SetExito(0));

        public Task<ResultadoOperacion<bool>> MarcarLeidaAsync(Guid usuarioId, Guid notificacionId) =>
            Task.FromResult(ResultadoOperacion<bool>.SetExito(true));

        public Task<ResultadoOperacion<int>> MarcarTodasLeidasAsync(Guid usuarioId) =>
            Task.FromResult(ResultadoOperacion<int>.SetExito(0));
    }

    /// <summary>Google Auth: nunca se usa en las pruebas de login por correo/clave.</summary>
    public class GoogleAuthServicioFalso : IGoogleAuthServicio
    {
        public Task<ResultadoOperacion<GoogleTokenPayload>> ValidarIdTokenAsync(string idToken) =>
            Task.FromResult(ResultadoOperacion<GoogleTokenPayload>.SetError("No usado en pruebas."));
    }

    /// <summary>Email transaccional: simula que el envío siempre funciona.</summary>
    public class EmailServicioFalso : IEmailServicio
    {
        public Task<bool> EnviarAsync(string destinatario, string nombreDestinatario,
            PlantillaCorreo plantilla, Dictionary<string, string> placeholders) =>
            Task.FromResult(true);

        public Task<bool> EnviarPruebaAsync(string destinatario, string nombreDestinatario) =>
            Task.FromResult(true);

        public Task<bool> EnviarHtmlAsync(string destinatario, string nombreDestinatario,
            string asunto, string cuerpoHtml) =>
            Task.FromResult(true);
    }

    public class AuditoriaServicioFalso : IAuditoriaServicio
    {
        public Task RegistrarAsync(string accion, Guid? usuarioId, string? direccionIp, string? detalle) =>
            Task.CompletedTask;
    }
}
