using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;

namespace TiendaVirtual.Dominio.Servicios.SuscripcionXqm
{
    public interface ICuponServicio
    {
        Task<ResultadoOperacion<List<CuponDto>>> ListarAsync();
        Task<ResultadoOperacion<CuponDto>> CrearAsync(CrearCuponDto dto);
        Task<ResultadoOperacion<CuponDto>> ActualizarAsync(int id, ActualizarCuponDto dto);
        Task<ResultadoOperacion<bool>> DesactivarAsync(int id);
        Task<ResultadoOperacion<CuponValidadoDto>> ValidarAsync(int usuarioId, ValidarCuponDto dto);
    }
}
