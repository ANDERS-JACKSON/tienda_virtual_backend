using System.Collections.Generic;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Servicios.VentaXqm
{
    public interface IMetodoEnvioServicio
    {
        Task<ResultadoOperacion<List<MetodoEnvioDto>>> ListarActivosAsync();
        Task<ResultadoOperacion<List<MetodoEnvioAdminDto>>> ListarTodosAdminAsync();
        Task<ResultadoOperacion<MetodoEnvioAdminDto>> CrearAsync(CrearMetodoEnvioDto dto);
        Task<ResultadoOperacion<MetodoEnvioAdminDto>> ActualizarAsync(int id, ActualizarMetodoEnvioDto dto);
        Task<ResultadoOperacion<bool>> ActivarAsync(int id);
        Task<ResultadoOperacion<bool>> DesactivarAsync(int id);
    }
}
