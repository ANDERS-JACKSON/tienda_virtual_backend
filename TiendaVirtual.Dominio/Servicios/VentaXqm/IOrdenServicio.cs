using System.Threading.Tasks;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Servicios.VentaXqm
{
    public interface IOrdenServicio
    {
        Task<ResultadoOperacion<OrdenDto>> CrearAsync(int usuarioId, CrearOrdenDto dto);
        Task<ResultadoOperacion<PaginacionRespuestaDto<OrdenListadoDto>>> ListarMisOrdenesAsync(
            int usuarioId, int pagina, int tamanioPagina);
        Task<ResultadoOperacion<OrdenDto>> ObtenerMiOrdenAsync(int usuarioId, long ordenId);
    }
}
