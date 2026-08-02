using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Servicios.VentaXqm
{
    public interface ICuponPedidoServicio
    {
        Task<ResultadoOperacion<List<CuponPedidoDto>>> ListarAsync();
        Task<ResultadoOperacion<CuponPedidoDto>> CrearAsync(CrearCuponPedidoDto dto);
        Task<ResultadoOperacion<CuponPedidoDto>> ActualizarAsync(int id, ActualizarCuponPedidoDto dto);
        Task<ResultadoOperacion<bool>> ActivarAsync(int id);
        Task<ResultadoOperacion<bool>> DesactivarAsync(int id);
    }
}
