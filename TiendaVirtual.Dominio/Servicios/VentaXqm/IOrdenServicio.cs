using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Servicios.VentaXqm
{
    public interface IOrdenServicio
    {
        Task<ResultadoOperacion<OrdenDto>> CrearAsync(Guid usuarioId, CrearOrdenDto dto);
        Task<ResultadoOperacion<PaginacionRespuestaDto<OrdenListadoDto>>> ListarMisOrdenesAsync(
            Guid usuarioId, int pagina, int tamanioPagina);
        Task<ResultadoOperacion<OrdenDto>> ObtenerMiOrdenAsync(Guid usuarioId, Guid ordenId);
        Task<ResultadoOperacion<bool>> CambiarEstadoSubordenAsync(
            Guid vendedorUsuarioId, Guid subordenId, TipoEstadoSuborden nuevoEstado);
        Task<ResultadoOperacion<EnvioDto>> RegistrarEnvioSubordenAsync(
            Guid vendedorUsuarioId, Guid subordenId, RegistrarEnvioSubordenDto dto);
        Task<ResultadoOperacion<bool>> MarcarListoParaRecogerAsync(
            Guid vendedorUsuarioId, Guid subordenId, MarcarListoParaRecogerDto? dto);

        // Admin
        Task<ResultadoOperacion<PaginacionRespuestaDto<OrdenAdminListadoDto>>> ListarAdminAsync(
            string? busqueda, TipoEstadoOrden? estado, DateTime? fechaDesde, DateTime? fechaHasta,
            int pagina, int tamanioPagina);
        Task<ResultadoOperacion<OrdenAdminResumenDto>> ObtenerResumenAdminAsync();
        Task<ResultadoOperacion<OrdenDto>> ObtenerAdminDetalleAsync(Guid ordenId);
        Task<ResultadoOperacion<bool>> CancelarAdminAsync(Guid ordenId, CancelarOrdenAdminDto dto);
    }
}
