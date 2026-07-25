using System.Threading.Tasks;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Servicios.VentaXqm
{
    public interface ICarritoServicio
    {
        Task<ResultadoOperacion<CarritoDto>> ObtenerMiCarritoAsync(Guid usuarioId);
        Task<ResultadoOperacion<CarritoDto>> AgregarItemAsync(Guid usuarioId, AgregarItemCarritoDto dto);
        Task<ResultadoOperacion<CarritoDto>> ActualizarItemAsync(Guid usuarioId, int itemId, ActualizarItemCarritoDto dto);
        Task<ResultadoOperacion<CarritoDto>> QuitarItemAsync(Guid usuarioId, int itemId);
        Task<ResultadoOperacion<bool>> VaciarAsync(Guid usuarioId);
    }
}
