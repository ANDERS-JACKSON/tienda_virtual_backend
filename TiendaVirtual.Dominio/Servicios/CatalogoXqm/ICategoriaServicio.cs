using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;

namespace TiendaVirtual.Dominio.Servicios.CatalogoXqm
{
    public interface ICategoriaServicio
    {
        Task<ResultadoOperacion<List<CategoriaDto>>> ListarAsync(bool soloActivas = true);
        Task<ResultadoOperacion<List<CategoriaArbolDto>>> ObtenerArbolAsync();
        Task<ResultadoOperacion<CategoriaDto>> ObtenerPorIdAsync(int id);
        Task<ResultadoOperacion<CategoriaDto>> CrearAsync(CrearCategoriaDto dto);
        Task<ResultadoOperacion<CategoriaDto>> ActualizarAsync(int id, ActualizarCategoriaDto dto);
        Task<ResultadoOperacion<bool>> DesactivarAsync(int id);
    }
}
