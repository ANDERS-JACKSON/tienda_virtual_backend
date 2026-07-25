using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.SoporteXqm;

namespace TiendaVirtual.Dominio.Servicios.SoporteXqm
{
    public interface IReclamoServicio
    {
        Task<ResultadoOperacion<ReclamoDto>> AbrirAsync(Guid usuarioId, AbrirReclamoDto dto);
        Task<ResultadoOperacion<ReclamoDto>> ObtenerDetalleAsync(Guid usuarioId, Guid reclamoId);
        Task<ResultadoOperacion<MensajeReclamoDto>> AgregarMensajeAsync(
            Guid usuarioId, Guid reclamoId, AgregarMensajeReclamoDto dto);
        Task<ResultadoOperacion<bool>> ResolverAsync(Guid usuarioId, Guid reclamoId, ResolverReclamoDto dto);
        Task<ResultadoOperacion<PaginacionRespuestaDto<ReclamoListadoDto>>> ListarMisAsync(
            Guid usuarioId, int pagina, int tamanioPagina);
        Task<ResultadoOperacion<PaginacionRespuestaDto<ReclamoListadoDto>>> ListarRecibidosAsync(
            Guid usuarioId, int pagina, int tamanioPagina);
        Task<ResultadoOperacion<PaginacionRespuestaDto<ReclamoListadoDto>>> ListarAdminAsync(
            int? estado, int pagina, int tamanioPagina);
    }
}
