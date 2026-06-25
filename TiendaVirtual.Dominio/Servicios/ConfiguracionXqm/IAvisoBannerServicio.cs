using System.Collections.Generic;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.ConfiguracionXqm;

namespace TiendaVirtual.Dominio.Servicios.ConfiguracionXqm
{
    public interface IAvisoBannerServicio
    {
        Task<ResultadoOperacion<List<AvisoBannerDto>>> ListarActivosAsync();
        Task<ResultadoOperacion<List<AvisoBannerAdminDto>>> ListarTodosAdminAsync();
        Task<ResultadoOperacion<AvisoBannerAdminDto>> CrearAsync(CrearAvisoBannerDto dto);
        Task<ResultadoOperacion<AvisoBannerAdminDto>> ActualizarAsync(int id, ActualizarAvisoBannerDto dto);
        Task<ResultadoOperacion<bool>> ActivarAsync(int id);
        Task<ResultadoOperacion<bool>> DesactivarAsync(int id);
        Task<ResultadoOperacion<bool>> EliminarAsync(int id);
    }
}
