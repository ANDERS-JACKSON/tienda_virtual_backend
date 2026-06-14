using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TiendaVirtual.Dominio.Servicios.CatalogoXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Api.Controllers.CatalogoXqm
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "CLIENTE,VENDEDOR,ADMIN,VERIFICADOR")]
    public class FavoritoController : ControllerBase
    {
        private readonly IFavoritoServicio _servicio;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FavoritoController(IFavoritoServicio servicio, IHttpContextAccessor httpContextAccessor)
        {
            _servicio = servicio;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        public async Task<ActionResult<ResultadoOperacion<PaginacionRespuestaDto<FavoritoDto>>>> Listar(
            [FromQuery] int pagina = 1, [FromQuery] int tamanioPagina = 12)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ListarMisFavoritosAsync(uid.Value, pagina, tamanioPagina);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("{productoId:int}")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Agregar(int productoId)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.AgregarAsync(uid.Value, productoId);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpDelete("{productoId:int}")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Quitar(int productoId)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.QuitarAsync(uid.Value, productoId);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        private int? ObtenerUsuarioId()
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(claim?.Value, out var id) ? id : null;
        }
    }
}
