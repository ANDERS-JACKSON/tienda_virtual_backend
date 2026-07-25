using System.Collections.Generic;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.SoporteXqm;

namespace TiendaVirtual.Dominio.Servicios.SoporteXqm
{
    public interface INotificacionServicio
    {
        Task CrearAsync(Guid usuarioId, string tipo, string titulo, string cuerpo,
            object? datos = null,
            PlantillaCorreo? plantillaEmail = null,
            Dictionary<string, string>? placeholdersEmail = null);

        Task<ResultadoOperacion<PaginacionRespuestaDto<NotificacionDto>>> ListarMisAsync(
            Guid usuarioId, int pagina, int tamanioPagina);

        Task<ResultadoOperacion<int>> ContarNoLeidasAsync(Guid usuarioId);

        Task<ResultadoOperacion<bool>> MarcarLeidaAsync(Guid usuarioId, Guid notificacionId);

        Task<ResultadoOperacion<int>> MarcarTodasLeidasAsync(Guid usuarioId);
    }
}
