using System.Security.Claims;
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

        [HttpPost("google")]
        [AllowAnonymous]
        public async Task<ActionResult<ResultadoOperacion<LoginRespuestaDto>>> LoginConGoogle(
            [FromBody] GoogleLoginDto dto)
        {
            var (ip, agente) = ObtenerDatosCliente();
            var resultado = await _servicio.LoginConGoogleAsync(dto, ip, agente);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPost("google/completar-registro")]
        [AllowAnonymous]
        public async Task<ActionResult<ResultadoOperacion<LoginRespuestaDto>>> CompletarRegistroGoogle(
            [FromBody] CompletarRegistroGoogleDto dto)
        {
            var (ip, agente) = ObtenerDatosCliente();
            var resultado = await _servicio.CompletarRegistroGoogleAsync(dto, ip, agente);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpGet("cuenta/seguridad")]
        [Authorize]
        public async Task<ActionResult<ResultadoOperacion<CuentaSeguridadDto>>> ObtenerSeguridadCuenta()
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            var resultado = await _servicio.ObtenerSeguridadCuentaAsync(usuarioId.Value);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPost("google/vincular")]
        [Authorize]
        public async Task<ActionResult<ResultadoOperacion<bool>>> VincularGoogle([FromBody] GoogleLoginDto dto)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            var resultado = await _servicio.VincularGoogleAsync(usuarioId.Value, dto);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPost("google/desvincular")]
        [Authorize]
        public async Task<ActionResult<ResultadoOperacion<bool>>> DesvincularGoogle(
            [FromBody] DesvincularGoogleDto dto)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            var resultado = await _servicio.DesvincularGoogleAsync(
                usuarioId.Value, dto?.Contrasena ?? string.Empty);
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
        public async Task<ActionResult<ResultadoOperacion<RegistroRespuestaDto>>> RegistrarCliente(
            [FromBody] RegistroClienteDto dto)
        {
            var (ip, agente) = ObtenerDatosCliente();
            var resultado = await _servicio.RegistrarClienteAsync(dto, ip, agente);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPost("registrar-vendedor")]
        [AllowAnonymous]
        public async Task<ActionResult<ResultadoOperacion<RegistroRespuestaDto>>> RegistrarVendedor(
            [FromBody] RegistroVendedorDto dto)
        {
            var (ip, agente) = ObtenerDatosCliente();
            var resultado = await _servicio.RegistrarVendedorAsync(dto, ip, agente);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPost("registrar-administrador")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<RegistroRespuestaDto>>> RegistrarAdministrador(
            [FromBody] RegistroAdministradorDto dto)
        {
            var (ip, agente) = ObtenerDatosCliente();
            var resultado = await _servicio.RegistrarAdministradorAsync(dto, ip, agente);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        /// <summary>
        /// "Olvidé mi contraseña". El usuario provee su correo; el sistema
        /// genera una clave nueva (que invalida la anterior) y se la envía
        /// al correo registrado.
        /// </summary>
        [HttpPost("recuperar-clave")]
        [AllowAnonymous]
        public async Task<ActionResult<ResultadoOperacion<string>>> RecuperarClave(
            [FromBody] RecuperarClaveDto dto)
        {
            var resultado = await _servicio.RecuperarClaveAsync(dto?.Correo ?? string.Empty);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        /// <summary>
        /// Primer cambio de contraseña tras el registro (sin iniciar sesión).
        /// </summary>
        [HttpPost("establecer-contrasena-inicial")]
        [AllowAnonymous]
        public async Task<ActionResult<ResultadoOperacion<bool>>> EstablecerContrasenaInicial(
            [FromBody] EstablecerContrasenaInicialDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var resultado = await _servicio.EstablecerContrasenaInicialAsync(
                dto.Correo,
                dto.ContrasenaActual,
                dto.ContrasenaNueva);

            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        /// <summary>
        /// Cambio de contraseña con sesión activa (perfil o tras login obligatorio).
        /// </summary>
        [HttpPut("cambiar-password")]
        [Authorize]
        public async Task<ActionResult<ResultadoOperacion<bool>>> CambiarPassword(
            [FromBody] CambiarPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(usuarioIdStr, out int usuarioId))
                return Unauthorized();

            var resultado = await _servicio.CambiarPasswordAsync(
                usuarioId,
                dto.ContrasenaActual,
                dto.ContrasenaNueva);

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

        private int? ObtenerUsuarioId()
        {
            var usuarioIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(usuarioIdStr, out int usuarioId) ? usuarioId : null;
        }
    }
}
