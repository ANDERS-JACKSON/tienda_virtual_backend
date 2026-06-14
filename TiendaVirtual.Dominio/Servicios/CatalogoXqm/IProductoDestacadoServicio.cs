using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;

namespace TiendaVirtual.Dominio.Servicios.CatalogoXqm
{
    public interface IProductoDestacadoServicio
    {
        Task<ResultadoOperacion<List<ProductoDestacadoPublicoDto>>> ListarPublicoAsync();
        Task<ResultadoOperacion<List<ProductoDestacadoAdminDto>>> ListarAdminAsync();
        Task<ResultadoOperacion<ProductoDestacadoAdminDto>> AgregarAsync(AgregarProductoDestacadoDto dto);
        Task<ResultadoOperacion<bool>> EliminarAsync(int destacadoId);
        Task<ResultadoOperacion<bool>> ReordenarAsync(ReordenarProductosDestacadosDto dto);
    }
}
