using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;

namespace TiendaVirtual.Dominio.Servicios.SuscripcionXqm
{
    public interface IPlanServicio
    {
        Task<ResultadoOperacion<List<PlanDto>>> ListarActivosAsync();
        Task<ResultadoOperacion<List<PlanDto>>> ListarTodosAsync();
        Task<ResultadoOperacion<PlanDto>> ObtenerPorIdAsync(int id);
        Task<ResultadoOperacion<PlanDto>> CrearAsync(CrearPlanDto dto);
        Task<ResultadoOperacion<PlanDto>> ActualizarAsync(int id, ActualizarPlanDto dto);
        Task<ResultadoOperacion<bool>> CambiarEstadoAsync(int id, bool activo);
    }
}
