using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;

namespace TiendaVirtual.Dominio.Servicios.SuscripcionXqm
{
    public interface ISuscripcionServicio
    {
        Task<ResultadoOperacion<SuscripcionDto?>> ObtenerMiSuscripcionAsync(Guid usuarioId);
        Task<ResultadoOperacion<SuscripcionElegibilidadDto>> ObtenerElegibilidadAsync(Guid usuarioId);
        Task<ResultadoOperacion<SuscripcionDto>> IniciarAsync(Guid usuarioId, IniciarSuscripcionDto dto);
        Task<ResultadoOperacion<SuscripcionDto>> ReactivarPlanAsync(Guid usuarioId, IniciarSuscripcionDto dto);
        Task<ResultadoOperacion<SuscripcionDto>> CambiarPlanAsync(Guid usuarioId, CambiarPlanDto dto);
        Task<ResultadoOperacion<bool>> CancelarAsync(Guid usuarioId);
        Task<bool> PuedeVendedorPublicarAsync(int vendedorId);
        Task<ResultadoOperacion<PaginacionRespuestaDto<SuscripcionDto>>> ListarAdminAsync(int pagina, int tamanioPagina);
        Task<ResultadoOperacion<bool>> SuspenderAsync(int suscripcionId);
        Task<ResultadoOperacion<bool>> ReactivarAsync(int suscripcionId);
        Task<ResultadoOperacion<int>> ProcesarVencimientosAsync();
    }
}
