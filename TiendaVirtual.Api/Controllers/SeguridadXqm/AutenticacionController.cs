using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Dominio.Servicios.SeguridadXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;

namespace TiendaVirtual.Api.Controllers.SeguridadXqm
{
    [ApiController]
    [Route("api/[controller]")]
    public class AutenticacionController : ControllerBase
    {
        private readonly IAutenticacionServicio _servicio;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AutenticacionController(
            IAutenticacionServicio servicio,
            IHttpContextAccessor httpContextAccessor)
        {
            _servicio = servicio;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<ResultadoOperacion<LoginRespuestaDto>>> Login(
            [FromBody] LoginDto dto)
        {
            var (ip, agente) = ObtenerDatosCliente();
            var resultado = await _servicio.LoginAsync(dto, ip, agente);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPost("login/verificar-2fa")]
        [AllowAnonymous]
        public async Task<ActionResult<ResultadoOperacion<TokenRespuestaDto>>> VerificarDosFactores(
            [FromBody] Verificar2FaRequestDto dto)
        {
            var (ip, agente) = ObtenerDatosCliente();
            var resultado = await _servicio.VerificarDosFactoresAsync(dto, ip, agente);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPost("registrar-cliente")]
        [AllowAnonymous]
        public async Task<ActionResult<ResultadoOperacion<TokenRespuestaDto>>> RegistrarCliente(
            [FromBody] RegistroClienteDto dto)
        {
            var (ip, agente) = ObtenerDatosCliente();
            var resultado = await _servicio.RegistrarClienteAsync(dto, ip, agente);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPost("registrar-vendedor")]
        [AllowAnonymous]
        public async Task<ActionResult<ResultadoOperacion<TokenRespuestaDto>>> RegistrarVendedor(
            [FromBody] RegistroVendedorDto dto)
        {
            var (ip, agente) = ObtenerDatosCliente();
            var resultado = await _servicio.RegistrarVendedorAsync(dto, ip, agente);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPost("registrar-administrador")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<TokenRespuestaDto>>> RegistrarAdministrador(
            [FromBody] RegistroAdministradorDto dto)
        {
            var (ip, agente) = ObtenerDatosCliente();
            var resultado = await _servicio.RegistrarAdministradorAsync(dto, ip, agente);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPost("refrescar")]
        [AllowAnonymous]
        public async Task<ActionResult<ResultadoOperacion<TokenRespuestaDto>>> Refrescar(
            [FromBody] RefrescarTokenDto dto)
        {
            var (ip, agente) = ObtenerDatosCliente();
            var resultado = await _servicio.RefrescarTokenAsync(dto.RefreshToken, ip, agente);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Logout(
            [FromBody] RefrescarTokenDto dto)
        {
            var resultado = await _servicio.LogoutAsync(dto.RefreshToken);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        private (string? Ip, string? Agente) ObtenerDatosCliente()
        {
            var http = _httpContextAccessor.HttpContext;
            var ip = http?.Connection.RemoteIpAddress?.ToString();
            var agente = http?.Request.Headers["User-Agent"].ToString();
            return (ip, agente);
        }
    }
}
