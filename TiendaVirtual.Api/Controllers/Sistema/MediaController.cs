using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Dominio;
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
        private readonly TiendaVirtualDbContext _db;

        public MediaController(ICloudinaryFirmaService cloudinary, TiendaVirtualDbContext db)
        {
            _cloudinary = cloudinary;
            _db = db;
        }

        /// <summary>
        /// Firma una subida a Cloudinary con carpeta según el tipo y el vendedor/cliente.
        /// Si Cloudinary:ApiSecret está vacío, responde { activo: false } y el folder
        /// para que el frontend intente el preset unsigned.
        /// </summary>
        [HttpPost("firma")]
        [EnableRateLimiting("media")]
        public ActionResult<ResultadoOperacion<FirmaSubidaMediaDto>> Firmar(
            [FromBody] SolicitarFirmaMediaDto? dto)
        {
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value);
            Guid? usuarioId = null;
            if (Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid))
                usuarioId = uid;

            int? vendedorId = null;
            if (usuarioId != null)
            {
                vendedorId = _db.Vendedores.AsNoTracking()
                    .Where(v => v.UsuarioId == usuarioId)
                    .Select(v => (int?)v.VendedorId)
                    .FirstOrDefault();
            }

            var r = _cloudinary.Firmar(dto?.Proposito, roles, usuarioId, vendedorId);
            return r.Exito ? Ok(r) : BadRequest(r);
        }
    }
}
