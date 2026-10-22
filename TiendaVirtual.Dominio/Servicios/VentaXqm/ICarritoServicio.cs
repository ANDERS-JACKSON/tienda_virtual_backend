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
        Task<ResultadoOperacion<CarritoDto>> AplicarCuponAsync(Guid usuarioId, AplicarCuponCarritoDto dto);
        Task<ResultadoOperacion<CarritoDto>> QuitarCuponAsync(Guid usuarioId);
        Task<ResultadoOperacion<RevisionCuponCarritoDto>> RevisarCuponAsync(RevisarCuponCarritoDto dto);

        /// <summary>Precio y stock actuales de un carrito que aún no tiene cuenta.</summary>
        Task<ResultadoOperacion<CarritoDto>> CotizarAsync(CotizarCarritoDto dto);

        /// <summary>Suma el carrito del navegador al carrito de la cuenta, sin duplicar líneas.</summary>
        Task<ResultadoOperacion<ResultadoFusionCarritoDto>> FusionarAsync(Guid usuarioId, CotizarCarritoDto dto);
    }
}
