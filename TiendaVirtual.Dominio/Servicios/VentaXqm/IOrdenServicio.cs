using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Servicios.VentaXqm
{
    public interface IOrdenServicio
    {
        Task<ResultadoOperacion<OrdenDto>> CrearAsync(int usuarioId, CrearOrdenDto dto);
        Task<ResultadoOperacion<PaginacionRespuestaDto<OrdenListadoDto>>> ListarMisOrdenesAsync(
            int usuarioId, int pagina, int tamanioPagina);
        Task<ResultadoOperacion<OrdenDto>> ObtenerMiOrdenAsync(int usuarioId, long ordenId);
        Task<ResultadoOperacion<bool>> CambiarEstadoSubordenAsync(
            int vendedorUsuarioId, long subordenId, TipoEstadoSuborden nuevoEstado);
        Task<ResultadoOperacion<EnvioDto>> RegistrarEnvioSubordenAsync(
            int vendedorUsuarioId, long subordenId, RegistrarEnvioSubordenDto dto);
        Task<ResultadoOperacion<bool>> MarcarListoParaRecogerAsync(
            int vendedorUsuarioId, long subordenId, MarcarListoParaRecogerDto? dto);

        // Admin
        Task<ResultadoOperacion<PaginacionRespuestaDto<OrdenAdminListadoDto>>> ListarAdminAsync(
            string? busqueda, TipoEstadoOrden? estado, DateTime? fechaDesde, DateTime? fechaHasta,
            int pagina, int tamanioPagina);
        Task<ResultadoOperacion<OrdenAdminResumenDto>> ObtenerResumenAdminAsync();
        Task<ResultadoOperacion<OrdenDto>> ObtenerAdminDetalleAsync(long ordenId);
        Task<ResultadoOperacion<bool>> CancelarAdminAsync(long ordenId, CancelarOrdenAdminDto dto);
    }
}
