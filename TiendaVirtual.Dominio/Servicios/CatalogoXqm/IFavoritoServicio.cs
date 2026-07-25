using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Dominio.Servicios.CatalogoXqm
{
    public interface IFavoritoServicio
    {
        Task<ResultadoOperacion<PaginacionRespuestaDto<FavoritoDto>>> ListarMisFavoritosAsync(
            Guid usuarioId, int pagina, int tamanioPagina);
        Task<ResultadoOperacion<bool>> AgregarAsync(Guid usuarioId, int productoId);
        Task<ResultadoOperacion<bool>> QuitarAsync(Guid usuarioId, int productoId);
    }
}
