using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Servicios.VentaXqm
{
    public interface IOrdenServicio
    {
        Task<ResultadoOperacion<OrdenDto>> CrearAsync(Guid usuarioId, CrearOrdenDto dto);

        /// <summary>
        /// Crea la orden y reserva stock sin vaciar el carrito ni notificar.
        /// Uso exclusivo del cobro atómico; si el pago falla se debe anular.
        /// </summary>
        Task<ResultadoOperacion<OrdenDto>> CrearReservandoParaCobroAsync(Guid usuarioId, CrearOrdenDto dto);

        /// <summary>
        /// Anula una orden PendientePago creada para cobro, libera stock y revierte cupón.
        /// El carrito se conserva (nunca se vació).
        /// </summary>
        Task<ResultadoOperacion<bool>> AnularReservaPendientePagoAsync(Guid usuarioId, Guid ordenId);

        /// <summary>Vacía el carrito del usuario tras cobro exitoso o pendiente.</summary>
        Task<ResultadoOperacion<bool>> VaciarCarritoTrasCobroAsync(Guid usuarioId);

        Task<ResultadoOperacion<PaginacionRespuestaDto<OrdenListadoDto>>> ListarMisOrdenesAsync(
            Guid usuarioId, int pagina, int tamanioPagina);
        Task<ResultadoOperacion<OrdenDto>> ObtenerMiOrdenAsync(Guid usuarioId, Guid ordenId);
        Task<ResultadoOperacion<bool>> CambiarEstadoSubordenAsync(
            Guid vendedorUsuarioId, Guid subordenId, TipoEstadoSuborden nuevoEstado);
        Task<ResultadoOperacion<EnvioDto>> RegistrarEnvioSubordenAsync(
            Guid vendedorUsuarioId, Guid subordenId, RegistrarEnvioSubordenDto dto);
        Task<ResultadoOperacion<bool>> MarcarListoParaRecogerAsync(
            Guid vendedorUsuarioId, Guid subordenId, MarcarListoParaRecogerDto? dto);

        // Admin
        Task<ResultadoOperacion<PaginacionRespuestaDto<OrdenAdminListadoDto>>> ListarAdminAsync(
            string? busqueda, TipoEstadoOrden? estado, DateTime? fechaDesde, DateTime? fechaHasta,
            int pagina, int tamanioPagina);
        Task<ResultadoOperacion<OrdenAdminResumenDto>> ObtenerResumenAdminAsync();
        Task<ResultadoOperacion<OrdenDto>> ObtenerAdminDetalleAsync(Guid ordenId);
        Task<ResultadoOperacion<bool>> CancelarAdminAsync(Guid ordenId, CancelarOrdenAdminDto dto);
    }
}
