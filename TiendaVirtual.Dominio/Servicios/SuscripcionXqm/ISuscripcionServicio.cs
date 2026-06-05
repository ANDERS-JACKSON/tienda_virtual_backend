using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;

namespace TiendaVirtual.Dominio.Servicios.SuscripcionXqm
{
    public interface ISuscripcionServicio
    {
        Task<ResultadoOperacion<SuscripcionDto?>> ObtenerMiSuscripcionAsync(int usuarioId);
        Task<ResultadoOperacion<SuscripcionElegibilidadDto>> ObtenerElegibilidadAsync(int usuarioId);
        Task<ResultadoOperacion<SuscripcionDto>> IniciarAsync(int usuarioId, IniciarSuscripcionDto dto);
        Task<ResultadoOperacion<SuscripcionDto>> ReactivarPlanAsync(int usuarioId, IniciarSuscripcionDto dto);
        Task<ResultadoOperacion<SuscripcionDto>> CambiarPlanAsync(int usuarioId, CambiarPlanDto dto);
        Task<ResultadoOperacion<bool>> CancelarAsync(int usuarioId);
        Task<bool> PuedeVendedorPublicarAsync(int vendedorId);
        Task<ResultadoOperacion<PaginacionRespuestaDto<SuscripcionDto>>> ListarAdminAsync(int pagina, int tamanioPagina);
        Task<ResultadoOperacion<bool>> SuspenderAsync(int suscripcionId);
        Task<ResultadoOperacion<bool>> ReactivarAsync(int suscripcionId);
        Task<ResultadoOperacion<int>> ProcesarVencimientosAsync();
    }
}
