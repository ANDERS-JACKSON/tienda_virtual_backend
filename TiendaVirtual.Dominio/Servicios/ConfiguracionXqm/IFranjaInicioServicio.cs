using System.Collections.Generic;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.ConfiguracionXqm;

namespace TiendaVirtual.Dominio.Servicios.ConfiguracionXqm
{
    public interface IFranjaInicioServicio
    {
        Task<ResultadoOperacion<List<FranjaInicioDto>>> ListarActivosAsync(string zona);
        Task<ResultadoOperacion<List<FranjaInicioAdminDto>>> ListarTodosAdminAsync(string? zona);
        Task<ResultadoOperacion<FranjaInicioAdminDto>> CrearAsync(CrearFranjaInicioDto dto);
        Task<ResultadoOperacion<FranjaInicioAdminDto>> ActualizarAsync(int id, ActualizarFranjaInicioDto dto);
        Task<ResultadoOperacion<bool>> EliminarAsync(int id);
    }
}
