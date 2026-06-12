using System.Threading.Tasks;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Dominio.Servicios.SeguridadXqm
{
    public interface IUsuarioAdminServicio
    {
        Task<ResultadoOperacion<PaginacionRespuestaDto<UsuarioAdminListadoDto>>> ListarAsync(
            string? busqueda, int? rolId, string? estado, int pagina, int tamanioPagina);
        Task<ResultadoOperacion<UsuarioAdminDetalleDto>> ObtenerDetalleAsync(int usuarioId);
        Task<ResultadoOperacion<bool>> ActivarAsync(int usuarioId);
        Task<ResultadoOperacion<bool>> DesactivarAsync(int usuarioId);
        Task<ResultadoOperacion<bool>> AsignarRolAsync(int usuarioId, int rolId);
        Task<ResultadoOperacion<bool>> QuitarRolAsync(int usuarioId, int rolId, int adminActualId);
        Task<ResultadoOperacion<bool>> ResetearClaveAsync(int usuarioId);
        Task<ResultadoOperacion<List<RolDto>>> ListarRolesAsync();
    }
}
