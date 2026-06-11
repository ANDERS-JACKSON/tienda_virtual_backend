using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.SoporteXqm;

namespace TiendaVirtual.Dominio.Servicios.SoporteXqm
{
    public interface IResenaServicio
    {
        Task<ResultadoOperacion<ResenaProductoDto>> CrearResenaProductoAsync(
            int usuarioId, CrearResenaProductoDto dto);
        Task<ResultadoOperacion<ResenaVendedorDto>> CrearResenaVendedorAsync(
            int usuarioId, CrearResenaVendedorDto dto);
        Task<ResultadoOperacion<ResenaProductoDto>> ResponderResenaProductoAsync(
            int usuarioId, long resenaId, ResponderResenaDto dto);
        Task<ResultadoOperacion<PaginacionRespuestaDto<ResenaProductoDto>>> ListarPorProductoAsync(
            int productoId, int pagina, int tamanioPagina);
        Task<ResultadoOperacion<PaginacionRespuestaDto<ResenaVendedorDto>>> ListarPorVendedorAsync(
            int vendedorId, int pagina, int tamanioPagina);
        Task<ResultadoOperacion<List<PendienteResenaDto>>> ObtenerPendientesAsync(int usuarioId);
    }
}
