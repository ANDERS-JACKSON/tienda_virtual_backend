using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.PagoXqm;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;

namespace TiendaVirtual.Dominio.Servicios.SuscripcionXqm
{
    public interface ISuscripcionPagoServicio
    {
        Task<ResultadoOperacion<RespuestaInicioPagoDto>> IniciarPagoAsync(Guid usuarioId, IniciarPagoSuscripcionDto dto);
        Task<ResultadoOperacion<TransaccionDto>> ConfirmarPagoAsync(ConfirmarPagoSuscripcionDto dto, Guid? usuarioIdSolicitante = null);
        Task<ResultadoOperacion<List<TransaccionDto>>> ListarMisTransaccionesAsync(Guid usuarioId);
    }
}
