using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;

namespace TiendaVirtual.Dominio.Servicios.SeguridadXqm
{
    public interface IUsuarioPerfilServicio
    {
        Task<ResultadoOperacion<UsuarioPerfilDto>> ObtenerMiPerfilAsync(Guid usuarioId);
        Task<ResultadoOperacion<UsuarioPerfilDto>> ActualizarMisDatosAsync(
            Guid usuarioId, ActualizarMisDatosPersonalesDto dto);
    }
}
