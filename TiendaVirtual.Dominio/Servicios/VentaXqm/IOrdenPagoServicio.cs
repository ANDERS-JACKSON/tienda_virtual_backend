using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Servicios.VentaXqm
{
    public interface IOrdenPagoServicio
    {
        Task<ResultadoOperacion<RespuestaInicioPagoOrdenDto>> IniciarPagoAsync(Guid usuarioId, IniciarPagoOrdenDto dto);
        Task<ResultadoOperacion<TransaccionOrdenDto>> ConfirmarPagoAsync(ConfirmarPagoOrdenDto dto, Guid? usuarioIdSolicitante = null);
    }
}
