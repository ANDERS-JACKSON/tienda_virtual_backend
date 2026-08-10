using TiendaVirtual.Dominio.Servicios.PagoXqm.Modelos;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.PagoXqm;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Servicios.SuscripcionXqm
{
    public interface ISuscripcionPagoServicio
    {
        Task<ResultadoOperacion<RespuestaInicioPagoDto>> IniciarPagoAsync(Guid usuarioId, IniciarPagoSuscripcionDto dto);

        Task<ResultadoOperacion<ResultadoProcesarPagoOrdenDto>> ProcesarPagoAsync(
            Guid usuarioId, ProcesarPagoSuscripcionDto dto);

        Task<ResultadoOperacion<TransaccionDto>> ConfirmarPagoAsync(
            ConfirmarPagoSuscripcionDto dto, Guid? usuarioIdSolicitante = null);

        Task<ResultadoOperacion<TransaccionDto>> AplicarResultadoProveedorAsync(
            ResultadoPagoVerificadoDto resultado);

        Task<ResultadoOperacion<List<TransaccionDto>>> ListarMisTransaccionesAsync(Guid usuarioId);
    }
}
