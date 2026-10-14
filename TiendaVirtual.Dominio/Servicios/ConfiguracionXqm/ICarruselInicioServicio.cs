using System.Collections.Generic;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.ConfiguracionXqm;

namespace TiendaVirtual.Dominio.Servicios.ConfiguracionXqm
{
    public interface ICarruselInicioServicio
    {
        Task<ResultadoOperacion<List<CarruselInicioDto>>> ListarActivosAsync();
        Task<ResultadoOperacion<List<CarruselInicioAdminDto>>> ListarTodosAdminAsync();
        Task<ResultadoOperacion<CarruselInicioAdminDto>> CrearAsync(CrearCarruselInicioDto dto);
        Task<ResultadoOperacion<CarruselInicioAdminDto>> ActualizarAsync(int id, ActualizarCarruselInicioDto dto);
        Task<ResultadoOperacion<bool>> ActivarAsync(int id);
        Task<ResultadoOperacion<bool>> DesactivarAsync(int id);
        Task<ResultadoOperacion<bool>> EliminarAsync(int id);
    }
}
