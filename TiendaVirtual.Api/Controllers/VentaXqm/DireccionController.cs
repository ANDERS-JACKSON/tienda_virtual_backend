using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Dominio.Servicios.VentaXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;

namespace TiendaVirtual.Api.Controllers.VentaXqm
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "CLIENTE,VENDEDOR,ADMIN,VERIFICADOR")]
    public class DireccionController : ControllerBase
    {
        private readonly IDireccionServicio _servicio;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DireccionController(IDireccionServicio servicio, IHttpContextAccessor httpContextAccessor)
        {
            _servicio = servicio;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet("mis-direcciones")]
        public async Task<ActionResult<ResultadoOperacion<List<DireccionDto>>>> ListarMisDirecciones()
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ListarMisDireccionesAsync(uid.Value);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("mis-direcciones/{id:int}")]
        public async Task<ActionResult<ResultadoOperacion<DireccionDto>>> Obtener(int id)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ObtenerPorIdAsync(uid.Value, id);
            return r.Exito ? Ok(r) : NotFound(r);
        }

        [HttpPost]
        public async Task<ActionResult<ResultadoOperacion<DireccionDto>>> Crear(
            [FromBody] CrearDireccionDto dto)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.CrearAsync(uid.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ResultadoOperacion<DireccionDto>>> Actualizar(
            int id, [FromBody] ActualizarDireccionDto dto)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ActualizarAsync(uid.Value, id, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Eliminar(int id)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.EliminarAsync(uid.Value, id);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("{id:int}/predeterminada")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Predeterminada(int id)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.MarcarPredeterminadaAsync(uid.Value, id);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        private int? ObtenerUsuarioId()
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(claim?.Value, out var id) ? id : null;
        }
    }
}
