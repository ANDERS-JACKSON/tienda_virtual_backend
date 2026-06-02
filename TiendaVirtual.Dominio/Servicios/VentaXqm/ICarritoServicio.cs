using System.Threading.Tasks;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Servicios.VentaXqm
{
    public interface ICarritoServicio
    {
        Task<ResultadoOperacion<CarritoDto>> ObtenerMiCarritoAsync(int usuarioId);
        Task<ResultadoOperacion<CarritoDto>> AgregarItemAsync(int usuarioId, AgregarItemCarritoDto dto);
        Task<ResultadoOperacion<CarritoDto>> ActualizarItemAsync(int usuarioId, int itemId, ActualizarItemCarritoDto dto);
        Task<ResultadoOperacion<CarritoDto>> QuitarItemAsync(int usuarioId, int itemId);
        Task<ResultadoOperacion<bool>> VaciarAsync(int usuarioId);
    }
}
