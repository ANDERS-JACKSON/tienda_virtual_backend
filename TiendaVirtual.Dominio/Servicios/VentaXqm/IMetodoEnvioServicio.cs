using System.Collections.Generic;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Servicios.VentaXqm
{
    public interface IMetodoEnvioServicio
    {
        Task<ResultadoOperacion<List<MetodoEnvioDto>>> ListarActivosAsync();
    }
}
