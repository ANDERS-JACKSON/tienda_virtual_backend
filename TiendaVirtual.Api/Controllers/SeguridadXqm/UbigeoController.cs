using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Dominio.Servicios.SeguridadXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;

namespace TiendaVirtual.Api.Controllers.SeguridadXqm
{
    /// <summary>Catálogo ubigeo Perú (público, solo lectura).</summary>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class UbigeoController : ControllerBase
    {
        private readonly IUbigeoServicio _servicio;

        public UbigeoController(IUbigeoServicio servicio) => _servicio = servicio;

        [HttpGet("departamentos")]
        public async Task<ActionResult<ResultadoOperacion<List<UbigeoItemDto>>>> Departamentos()
        {
            var r = await _servicio.ListarDepartamentosAsync();
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("provincias")]
        public async Task<ActionResult<ResultadoOperacion<List<UbigeoItemDto>>>> Provincias(
            [FromQuery] string departamentoId)
        {
            var r = await _servicio.ListarProvinciasAsync(departamentoId);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("distritos")]
        public async Task<ActionResult<ResultadoOperacion<List<UbigeoItemDto>>>> Distritos(
            [FromQuery] string provinciaId)
        {
            var r = await _servicio.ListarDistritosAsync(provinciaId);
            return r.Exito ? Ok(r) : BadRequest(r);
        }
    }
}
