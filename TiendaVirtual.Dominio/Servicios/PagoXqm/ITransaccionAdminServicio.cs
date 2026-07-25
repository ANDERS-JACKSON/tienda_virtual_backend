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
        Task<ResultadoOperacion<TransaccionAdminDetalleDto>> ObtenerDetalleAsync(Guid transaccionId);
        Task<ResultadoOperacion<bool>> MarcarCompletadaAsync(Guid transaccionId);
        Task<ResultadoOperacion<bool>> MarcarFallidaAsync(Guid transaccionId, MarcarTransaccionFallidaDto dto);
    }
}
