using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.SoporteXqm;

namespace TiendaVirtual.Dominio.Servicios.SoporteXqm
{
    public interface IMensajeContactoServicio
    {
        Task<ResultadoOperacion<Guid>> CrearAsync(CrearMensajeContactoDto dto, Guid? usuarioIdSiLogueado);
        Task<ResultadoOperacion<MensajeContactoDetalleDto>> ObtenerDetalleAsync(Guid id);
        Task<ResultadoOperacion<PaginacionRespuestaDto<MensajeContactoListadoDto>>> ListarAsync(
            int pagina, int tamanio, int? estado, string? busqueda);
        Task<ResultadoOperacion<bool>> ResponderAsync(Guid id, Guid adminId, ResponderMensajeContactoDto dto);
        Task<ResultadoOperacion<bool>> CambiarEstadoAsync(Guid id, Guid adminId, TipoEstadoContacto nuevoEstado);
        Task<ResultadoOperacion<ContadorMensajesContactoDto>> ContarNoLeidosAsync();
    }
}
