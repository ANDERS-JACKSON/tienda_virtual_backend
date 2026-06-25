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
    public interface ICatalogoServicio
    {
        /// <summary>Listado público con filtros y paginación "cargar más".</summary>
        Task<ResultadoOperacion<PaginacionRespuestaDto<ProductoListadoDto>>> ListarAsync(FiltrosCatalogoDto filtros);

        /// <summary>Detalle público del producto por slug. Incrementa el contador de vistas.</summary>
        Task<ResultadoOperacion<ProductoDetalleDto>> ObtenerPorSlugAsync(string slug);

        /// <summary>Productos relacionados (misma categoría y mismo tipo: físico o patrón).</summary>
        Task<ResultadoOperacion<List<ProductoListadoDto>>> ObtenerRelacionadosAsync(string slug, int cantidad = 6);

        /// <summary>Productos públicos de un vendedor específico.</summary>
        Task<ResultadoOperacion<PaginacionRespuestaDto<ProductoListadoDto>>> ListarPorVendedorAsync(
            int vendedorId, int pagina, int tamanioPagina);
    }
}
