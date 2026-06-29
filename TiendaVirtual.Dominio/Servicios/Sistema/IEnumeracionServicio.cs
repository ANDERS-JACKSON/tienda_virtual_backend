using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Dominio.Servicios.Sistema
{
    public interface IEnumeracionServicio
    {
        Task<ResultadoOperacion<List<EnumeracionDto>>> ListarPorGrupoAsync(string grupo);
        Task<ResultadoOperacion<List<TipoDocumentoIdentidadDto>>> ListarTiposDocumentoIdentidadAsync();
    }
}
