using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TiendaVirtual.Dominio.Servicios.ConfiguracionXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.ConfiguracionXqm;

namespace TiendaVirtual.Api.Controllers.ConfiguracionXqm
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "ADMIN")]
    public class ConfiguracionCorreoController : ControllerBase
    {
        private readonly IConfiguracionCorreoServicio _servicio;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ConfiguracionCorreoController(
            IConfiguracionCorreoServicio servicio,
            IHttpContextAccessor httpContextAccessor)
        {
            _servicio = servicio;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        public async Task<ActionResult<ResultadoOperacion<ConfiguracionCorreoDto>>> Obtener()
        {
            var r = await _servicio.ObtenerAsync();
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPut("smtp")]
        public async Task<ActionResult<ResultadoOperacion<ConfiguracionCorreoDto>>> ActualizarSmtp(
            [FromBody] ActualizarSmtpDto dto)
        {
            var r = await _servicio.ActualizarSmtpAsync(dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPut("plantillas")]
        public async Task<ActionResult<ResultadoOperacion<ConfiguracionCorreoDto>>> ActualizarPlantillas(
            [FromBody] ActualizarPlantillasDto dto)
        {
            var r = await _servicio.ActualizarPlantillasAsync(dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("probar")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Probar()
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.EnviarPruebaAsync(uid.Value);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        private int? ObtenerUsuarioId()
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(claim?.Value, out var id) ? id : null;
        }
    }
}
