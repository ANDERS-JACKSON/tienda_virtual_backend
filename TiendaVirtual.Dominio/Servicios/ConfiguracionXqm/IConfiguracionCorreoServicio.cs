using System.Threading.Tasks;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.ConfiguracionXqm;

namespace TiendaVirtual.Dominio.Servicios.ConfiguracionXqm
{
    public interface IConfiguracionCorreoServicio
    {
        Task<ResultadoOperacion<ConfiguracionCorreoDto>> ObtenerAsync();
        Task<ResultadoOperacion<ConfiguracionCorreoDto>> ActualizarSmtpAsync(ActualizarSmtpDto dto);
        Task<ResultadoOperacion<ConfiguracionCorreoDto>> ActualizarPlantillasAsync(ActualizarPlantillasDto dto);
        Task<ResultadoOperacion<bool>> EnviarPruebaAsync(int adminUsuarioId);
    }
}
