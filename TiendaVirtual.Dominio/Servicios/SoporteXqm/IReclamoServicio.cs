using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.SoporteXqm;

namespace TiendaVirtual.Dominio.Servicios.SoporteXqm
{
    public interface IReclamoServicio
    {
        Task<ResultadoOperacion<ReclamoDto>> AbrirAsync(int usuarioId, AbrirReclamoDto dto);
        Task<ResultadoOperacion<ReclamoDto>> ObtenerDetalleAsync(int usuarioId, long reclamoId);
        Task<ResultadoOperacion<MensajeReclamoDto>> AgregarMensajeAsync(
            int usuarioId, long reclamoId, AgregarMensajeReclamoDto dto);
        Task<ResultadoOperacion<bool>> ResolverAsync(int usuarioId, long reclamoId, ResolverReclamoDto dto);
        Task<ResultadoOperacion<PaginacionRespuestaDto<ReclamoListadoDto>>> ListarMisAsync(
            int usuarioId, int pagina, int tamanioPagina);
        Task<ResultadoOperacion<PaginacionRespuestaDto<ReclamoListadoDto>>> ListarRecibidosAsync(
            int usuarioId, int pagina, int tamanioPagina);
        Task<ResultadoOperacion<PaginacionRespuestaDto<ReclamoListadoDto>>> ListarAdminAsync(
            int? estado, int pagina, int tamanioPagina);
    }
}
