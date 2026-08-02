using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;

namespace TiendaVirtual.Dominio.Servicios.SeguridadXqm
{
    public interface IUbigeoAdminServicio
    {
        Task<ResultadoOperacion<List<DepartamentoAdminDto>>> ListarDepartamentosAsync();
        Task<ResultadoOperacion<DepartamentoAdminDto>> CrearDepartamentoAsync(CrearDepartamentoDto dto);
        Task<ResultadoOperacion<DepartamentoAdminDto>> ActualizarDepartamentoAsync(string id, ActualizarDepartamentoDto dto);
        Task<ResultadoOperacion<bool>> EliminarDepartamentoAsync(string id);

        Task<ResultadoOperacion<List<ProvinciaAdminDto>>> ListarProvinciasAsync(string? departamentoId);
        Task<ResultadoOperacion<ProvinciaAdminDto>> CrearProvinciaAsync(CrearProvinciaDto dto);
        Task<ResultadoOperacion<ProvinciaAdminDto>> ActualizarProvinciaAsync(string id, ActualizarProvinciaDto dto);
        Task<ResultadoOperacion<bool>> EliminarProvinciaAsync(string id);

        Task<ResultadoOperacion<List<DistritoAdminDto>>> ListarDistritosAsync(string? provinciaId);
        Task<ResultadoOperacion<DistritoAdminDto>> CrearDistritoAsync(CrearDistritoDto dto);
        Task<ResultadoOperacion<DistritoAdminDto>> ActualizarDistritoAsync(string id, ActualizarDistritoDto dto);
        Task<ResultadoOperacion<bool>> EliminarDistritoAsync(string id);
    }
}
