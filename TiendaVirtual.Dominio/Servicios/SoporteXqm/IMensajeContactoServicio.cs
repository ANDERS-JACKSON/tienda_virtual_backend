using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.SoporteXqm;

namespace TiendaVirtual.Dominio.Servicios.SoporteXqm
{
    public interface IMensajeContactoServicio
    {
        Task<ResultadoOperacion<long>> CrearAsync(CrearMensajeContactoDto dto, int? usuarioIdSiLogueado);
        Task<ResultadoOperacion<MensajeContactoDetalleDto>> ObtenerDetalleAsync(long id);
        Task<ResultadoOperacion<PaginacionRespuestaDto<MensajeContactoListadoDto>>> ListarAsync(
            int pagina, int tamanio, int? estado, string? busqueda);
        Task<ResultadoOperacion<bool>> ResponderAsync(long id, int adminId, ResponderMensajeContactoDto dto);
        Task<ResultadoOperacion<bool>> CambiarEstadoAsync(long id, int adminId, TipoEstadoContacto nuevoEstado);
        Task<ResultadoOperacion<ContadorMensajesContactoDto>> ContarNoLeidosAsync();
    }
}
