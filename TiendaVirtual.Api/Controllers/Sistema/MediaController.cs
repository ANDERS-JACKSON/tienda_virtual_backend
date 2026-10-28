using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TiendaVirtual.Dominio.Servicios.Sistema;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Api.Controllers.Sistema
{
    [ApiController]
    [Route("api/media")]
    [Authorize]
    public class MediaController : ControllerBase
    {
        private readonly ICloudinaryFirmaService _cloudinary;

        public MediaController(ICloudinaryFirmaService cloudinary)
        {
            _cloudinary = cloudinary;
        }

        /// <summary>
        /// Firma una subida a Cloudinary. Si Cloudinary:ApiSecret está vacío,
        /// responde { activo: false } y el frontend usa el preset unsigned actual.
        /// </summary>
        [HttpPost("firma")]
        [EnableRateLimiting("media")]
        public ActionResult<ResultadoOperacion<FirmaSubidaMediaDto>> Firmar(
            [FromBody] SolicitarFirmaMediaDto? dto)
        {
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value);
            var r = _cloudinary.Firmar(dto?.Proposito, roles);
            return r.Exito ? Ok(r) : BadRequest(r);
        }
    }
}
