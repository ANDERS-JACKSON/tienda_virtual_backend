using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Api.Seguridad;
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

        public DireccionController(IDireccionServicio servicio)
        {
            _servicio = servicio;
        }

        [HttpGet("mis-direcciones")]
        public async Task<ActionResult<ResultadoOperacion<List<DireccionDto>>>> ListarMisDirecciones()
        {
            var uid = User.ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ListarMisDireccionesAsync(uid.Value);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("mis-direcciones/{id:guid}")]
        public async Task<ActionResult<ResultadoOperacion<DireccionDto>>> Obtener(Guid id)
        {
            var uid = User.ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            // Ownership: el servicio solo devuelve direcciones de la persona del usuario.
            var r = await _servicio.ObtenerPorIdAsync(uid.Value, id);
            return r.Exito ? Ok(r) : NotFound(r);
        }

        [HttpPost]
        public async Task<ActionResult<ResultadoOperacion<DireccionDto>>> Crear(
            [FromBody] CrearDireccionDto dto)
        {
            var uid = User.ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.CrearAsync(uid.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ResultadoOperacion<DireccionDto>>> Actualizar(
            Guid id, [FromBody] ActualizarDireccionDto dto)
        {
            var uid = User.ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ActualizarAsync(uid.Value, id, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Eliminar(Guid id)
        {
            var uid = User.ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.EliminarAsync(uid.Value, id);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("{id:guid}/predeterminada")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Predeterminada(Guid id)
        {
            var uid = User.ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.MarcarPredeterminadaAsync(uid.Value, id);
            return r.Exito ? Ok(r) : BadRequest(r);
        }
    }
}
