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
        Task<ResultadoOperacion<UsuarioAdminDetalleDto>> ObtenerDetalleAsync(Guid usuarioId);
        Task<ResultadoOperacion<bool>> ActivarAsync(Guid usuarioId);
        Task<ResultadoOperacion<bool>> DesactivarAsync(Guid usuarioId);
        Task<ResultadoOperacion<bool>> AsignarRolAsync(Guid usuarioId, int rolId);
        Task<ResultadoOperacion<bool>> QuitarRolAsync(Guid usuarioId, int rolId, Guid adminActualId);
        Task<ResultadoOperacion<bool>> ResetearClaveAsync(Guid usuarioId);
        Task<ResultadoOperacion<List<RolDto>>> ListarRolesAsync();
    }
}
