using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.SoporteXqm;

namespace TiendaVirtual.Dominio.Servicios.SoporteXqm
{
    public interface IResenaServicio
    {
        Task<ResultadoOperacion<ResenaProductoDto>> CrearResenaProductoAsync(
            Guid usuarioId, CrearResenaProductoDto dto);
        Task<ResultadoOperacion<ResenaVendedorDto>> CrearResenaVendedorAsync(
            Guid usuarioId, CrearResenaVendedorDto dto);
        Task<ResultadoOperacion<ResenaProductoDto>> ResponderResenaProductoAsync(
            Guid usuarioId, long resenaId, ResponderResenaDto dto);
        Task<ResultadoOperacion<ResenaProductoResumenDto>> ObtenerResumenProductoAsync(int productoId);
        Task<ResultadoOperacion<PaginacionRespuestaDto<ResenaProductoDto>>> ListarPorProductoAsync(
            int productoId, int pagina, int tamanioPagina);
        Task<ResultadoOperacion<PaginacionRespuestaDto<ResenaVendedorDto>>> ListarPorVendedorAsync(
            int vendedorId, int pagina, int tamanioPagina);
        Task<ResultadoOperacion<List<PendienteResenaDto>>> ObtenerPendientesAsync(Guid usuarioId);
    }
}
