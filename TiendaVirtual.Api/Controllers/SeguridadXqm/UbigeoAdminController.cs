using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Dominio.Servicios.SeguridadXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;

namespace TiendaVirtual.Api.Controllers.SeguridadXqm
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "ADMIN")]
    public class UbigeoAdminController : ControllerBase
    {
        private readonly IUbigeoAdminServicio _servicio;

        public UbigeoAdminController(IUbigeoAdminServicio servicio) => _servicio = servicio;

        // ── Departamentos ──────────────────────────────────────

        [HttpGet("departamentos")]
        public async Task<ActionResult<ResultadoOperacion<List<DepartamentoAdminDto>>>> ListarDepartamentos()
        {
            var r = await _servicio.ListarDepartamentosAsync();
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("departamentos")]
        public async Task<ActionResult<ResultadoOperacion<DepartamentoAdminDto>>> CrearDepartamento(
            [FromBody] CrearDepartamentoDto dto)
        {
            var r = await _servicio.CrearDepartamentoAsync(dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPut("departamentos/{id}")]
        public async Task<ActionResult<ResultadoOperacion<DepartamentoAdminDto>>> ActualizarDepartamento(
            string id, [FromBody] ActualizarDepartamentoDto dto)
        {
            var r = await _servicio.ActualizarDepartamentoAsync(id, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpDelete("departamentos/{id}")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> EliminarDepartamento(string id)
        {
            var r = await _servicio.EliminarDepartamentoAsync(id);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        // ── Provincias ─────────────────────────────────────────

        [HttpGet("provincias")]
        public async Task<ActionResult<ResultadoOperacion<List<ProvinciaAdminDto>>>> ListarProvincias(
            [FromQuery] string? departamentoId)
        {
            var r = await _servicio.ListarProvinciasAsync(departamentoId);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("provincias")]
        public async Task<ActionResult<ResultadoOperacion<ProvinciaAdminDto>>> CrearProvincia(
            [FromBody] CrearProvinciaDto dto)
        {
            var r = await _servicio.CrearProvinciaAsync(dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPut("provincias/{id}")]
        public async Task<ActionResult<ResultadoOperacion<ProvinciaAdminDto>>> ActualizarProvincia(
            string id, [FromBody] ActualizarProvinciaDto dto)
        {
            var r = await _servicio.ActualizarProvinciaAsync(id, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpDelete("provincias/{id}")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> EliminarProvincia(string id)
        {
            var r = await _servicio.EliminarProvinciaAsync(id);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        // ── Distritos ──────────────────────────────────────────

        [HttpGet("distritos")]
        public async Task<ActionResult<ResultadoOperacion<List<DistritoAdminDto>>>> ListarDistritos(
            [FromQuery] string? provinciaId)
        {
            var r = await _servicio.ListarDistritosAsync(provinciaId);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("distritos")]
        public async Task<ActionResult<ResultadoOperacion<DistritoAdminDto>>> CrearDistrito(
            [FromBody] CrearDistritoDto dto)
        {
            var r = await _servicio.CrearDistritoAsync(dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPut("distritos/{id}")]
        public async Task<ActionResult<ResultadoOperacion<DistritoAdminDto>>> ActualizarDistrito(
            string id, [FromBody] ActualizarDistritoDto dto)
        {
            var r = await _servicio.ActualizarDistritoAsync(id, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpDelete("distritos/{id}")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> EliminarDistrito(string id)
        {
            var r = await _servicio.EliminarDistritoAsync(id);
            return r.Exito ? Ok(r) : BadRequest(r);
        }
    }
}
