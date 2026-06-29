using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Servicios.VentaXqm
{
    public interface IOrdenPagoServicio
    {
        Task<ResultadoOperacion<RespuestaInicioPagoOrdenDto>> IniciarPagoAsync(int usuarioId, IniciarPagoOrdenDto dto);
        Task<ResultadoOperacion<TransaccionOrdenDto>> ConfirmarPagoAsync(ConfirmarPagoOrdenDto dto, int? usuarioIdSolicitante = null);
    }
}
