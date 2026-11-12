using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;

namespace TiendaVirtual.Dominio.Servicios.CatalogoXqm
{
    public interface IOpcionVarianteServicio
    {
        Task<ResultadoOperacion<List<OpcionVarianteDto>>> ListarActivasAsync();
        Task<ResultadoOperacion<List<OpcionVarianteDto>>> ListarAdminAsync();
        Task<ResultadoOperacion<OpcionVarianteDto>> CrearAsync(CrearOpcionVarianteDto dto);
        Task<ResultadoOperacion<OpcionVarianteDto>> ActualizarAsync(int id, ActualizarOpcionVarianteDto dto);
        Task<ResultadoOperacion<bool>> ActivarAsync(int id);
        Task<ResultadoOperacion<bool>> DesactivarAsync(int id);
        Task<ResultadoOperacion<bool>> EliminarAsync(int id);
    }
}
