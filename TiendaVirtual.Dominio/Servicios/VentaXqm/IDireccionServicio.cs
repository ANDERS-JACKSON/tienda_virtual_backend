using System.Collections.Generic;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;

namespace TiendaVirtual.Dominio.Servicios.VentaXqm
{
    public interface IDireccionServicio
    {
        Task<ResultadoOperacion<List<DireccionDto>>> ListarMisDireccionesAsync(int usuarioId);
        Task<ResultadoOperacion<DireccionDto>> ObtenerPorIdAsync(int usuarioId, int direccionId);
        Task<ResultadoOperacion<DireccionDto>> CrearAsync(int usuarioId, CrearDireccionDto dto);
        Task<ResultadoOperacion<DireccionDto>> ActualizarAsync(int usuarioId, int direccionId, ActualizarDireccionDto dto);
        Task<ResultadoOperacion<bool>> EliminarAsync(int usuarioId, int direccionId);
        Task<ResultadoOperacion<bool>> MarcarPredeterminadaAsync(int usuarioId, int direccionId);
    }
}
