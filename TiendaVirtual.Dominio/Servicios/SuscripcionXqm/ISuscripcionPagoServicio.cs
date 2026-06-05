using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.PagoXqm;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;

namespace TiendaVirtual.Dominio.Servicios.SuscripcionXqm
{
    public interface ISuscripcionPagoServicio
    {
        Task<ResultadoOperacion<RespuestaInicioPagoDto>> IniciarPagoAsync(int usuarioId, IniciarPagoSuscripcionDto dto);
        Task<ResultadoOperacion<TransaccionDto>> ConfirmarPagoAsync(ConfirmarPagoSuscripcionDto dto, int? usuarioIdSolicitante = null);
        Task<ResultadoOperacion<List<TransaccionDto>>> ListarMisTransaccionesAsync(int usuarioId);
    }
}
