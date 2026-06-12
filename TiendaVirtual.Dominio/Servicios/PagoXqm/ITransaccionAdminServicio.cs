using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.PagoXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Dominio.Servicios.PagoXqm
{
    public interface ITransaccionAdminServicio
    {
        Task<ResultadoOperacion<PaginacionRespuestaDto<TransaccionAdminListadoDto>>> ListarAsync(
            TipoTransaccion? tipo, TipoEstadoTransaccion? estado,
            DateTime? fechaDesde, DateTime? fechaHasta, int pagina, int tamanioPagina);
        Task<ResultadoOperacion<TransaccionAdminResumenDto>> ObtenerResumenAsync();
        Task<ResultadoOperacion<TransaccionAdminDetalleDto>> ObtenerDetalleAsync(long transaccionId);
        Task<ResultadoOperacion<bool>> MarcarCompletadaAsync(long transaccionId);
        Task<ResultadoOperacion<bool>> MarcarFallidaAsync(long transaccionId, MarcarTransaccionFallidaDto dto);
    }
}
