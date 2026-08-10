using TiendaVirtual.Dominio.Servicios.PagoXqm.Modelos;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Servicios.VentaXqm
{
    public interface IOrdenPagoServicio
    {
        /// <summary>
        /// PublicKey / flags del proveedor activo + monto estimado del carrito.
        /// No crea orden ni vacía el carrito.
        /// </summary>
        Task<ResultadoOperacion<ConfiguracionCheckoutPagoDto>> ObtenerConfiguracionCheckoutAsync(Guid usuarioId);

        /// <summary>
        /// Crea la orden solo al momento de cobrar con token. Si el pago falla,
        /// anula la reserva y conserva el carrito.
        /// </summary>
        Task<ResultadoOperacion<ResultadoCobrarCarritoDto>> CobrarCarritoAsync(
            Guid usuarioId, CobrarCarritoDto dto);

        Task<ResultadoOperacion<RespuestaInicioPagoOrdenDto>> IniciarPagoAsync(Guid usuarioId, IniciarPagoOrdenDto dto);

        Task<ResultadoOperacion<ResultadoProcesarPagoOrdenDto>> ProcesarPagoAsync(
            Guid usuarioId, ProcesarPagoOrdenDto dto);

        /// <summary>
        /// Consulta el estado del cobro de una orden PendientePago (o confirma si ya pagó).
        /// Si hay un pago en curso en MP, solo consulta (no crea otro cobro).
        /// </summary>
        Task<ResultadoOperacion<ResultadoVerificarPagoOrdenDto>> VerificarPagoOrdenAsync(
            Guid usuarioId, VerificarPagoOrdenDto dto);

        /// <summary>
        /// Confirmación demo Izipay (solo Development) o legado. No usar para Mercado Pago real.
        /// </summary>
        Task<ResultadoOperacion<TransaccionOrdenDto>> ConfirmarPagoAsync(
            ConfirmarPagoOrdenDto dto, Guid? usuarioIdSolicitante = null);

        /// <summary>
        /// Aplica resultado ya verificado (webhook + GET al proveedor). Idempotente.
        /// </summary>
        Task<ResultadoOperacion<TransaccionOrdenDto>> AplicarResultadoProveedorAsync(
            ResultadoPagoVerificadoDto resultado);
    }
}
