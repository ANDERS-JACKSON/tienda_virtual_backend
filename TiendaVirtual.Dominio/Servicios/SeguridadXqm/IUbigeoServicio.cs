using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;

namespace TiendaVirtual.Dominio.Servicios.SeguridadXqm
{
    public interface IUbigeoServicio
    {
        Task<ResultadoOperacion<List<UbigeoItemDto>>> ListarDepartamentosAsync();
        Task<ResultadoOperacion<List<UbigeoItemDto>>> ListarProvinciasAsync(string departamentoId);
        Task<ResultadoOperacion<List<UbigeoItemDto>>> ListarDistritosAsync(string provinciaId);
    }
}
