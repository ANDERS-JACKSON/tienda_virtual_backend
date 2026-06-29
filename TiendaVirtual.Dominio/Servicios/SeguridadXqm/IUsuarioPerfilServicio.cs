using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;

namespace TiendaVirtual.Dominio.Servicios.SeguridadXqm
{
    public interface IUsuarioPerfilServicio
    {
        Task<ResultadoOperacion<UsuarioPerfilDto>> ObtenerMiPerfilAsync(int usuarioId);
        Task<ResultadoOperacion<UsuarioPerfilDto>> ActualizarMisDatosAsync(
            int usuarioId, ActualizarMisDatosPersonalesDto dto);
    }
}
