using System.Collections.Generic;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;

namespace TiendaVirtual.Dominio.Servicios.VentaXqm
{
    public interface IDireccionServicio
    {
        Task<ResultadoOperacion<List<DireccionDto>>> ListarMisDireccionesAsync(Guid usuarioId);
        Task<ResultadoOperacion<DireccionDto>> ObtenerPorIdAsync(Guid usuarioId, Guid direccionId);
        Task<ResultadoOperacion<DireccionDto>> CrearAsync(Guid usuarioId, CrearDireccionDto dto);
        Task<ResultadoOperacion<DireccionDto>> ActualizarAsync(Guid usuarioId, Guid direccionId, ActualizarDireccionDto dto);
        Task<ResultadoOperacion<bool>> EliminarAsync(Guid usuarioId, Guid direccionId);
        Task<ResultadoOperacion<bool>> MarcarPredeterminadaAsync(Guid usuarioId, Guid direccionId);
    }
}
