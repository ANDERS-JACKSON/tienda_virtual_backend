using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TiendaVirtual.Dominio.Servicios.SeguridadXqm;
using TiendaVirtual.Dominio.Servicios.Sistema;
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
        private readonly ITurnstileService _turnstile;

        public AutenticacionController(
            IAutenticacionServicio servicio,
            IHttpContextAccessor httpContextAccessor,
            ITurnstileService turnstile)
        {
            _servicio = servicio;
            _httpContextAccessor = httpContextAccessor;
            _turnstile = turnstile;
        }

        /// <summary>
        /// A4+M12: valida el captcha de Cloudflare Turnstile. Si Turnstile:Activo=false
        /// (por defecto en desarrollo), siempre pasa sin exigir nada al cliente.
        /// </summary>
        private async Task<ActionResult?> ValidarCaptchaAsync(string? token)
        {
            var (ip, _) = ObtenerDatosCliente();
            var valido = await _turnstile.ValidarAsync(token, ip);
            if (!valido)
            {
                return BadRequest(ResultadoOperacion<bool>.SetError(
                    "No pudimos verificar que eres una persona. Recarga la página e inténtalo de nuevo."));
            }
            return null;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        public async Task<ActionResult<ResultadoOperacion<LoginRespuestaDto>>> Login(
            [FromBody] LoginDto dto)
        {
            var errorCaptcha = await ValidarCaptchaAsync(dto?.TurnstileToken);
            if (errorCaptcha != null) return errorCaptcha;

            var (ip, agente) = ObtenerDatosCliente();
            var resultado = await _servicio.LoginAsync(dto!, ip, agente);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPost("google")]
        [AllowAnonymous]
        [EnableRateLimiting("google")]
        public async Task<ActionResult<ResultadoOperacion<LoginRespuestaDto>>> LoginConGoogle(
            [FromBody] GoogleLoginDto dto)
        {
            var (ip, agente) = ObtenerDatosCliente();
            var resultado = await _servicio.LoginConGoogleAsync(dto, ip, agente);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPost("google/completar-registro")]
        [AllowAnonymous]
        [EnableRateLimiting("google")]
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
        [EnableRateLimiting("dosFactores")]
        public async Task<ActionResult<ResultadoOperacion<TokenRespuestaDto>>> VerificarDosFactores(
            [FromBody] Verificar2FaRequestDto dto)
        {
            var (ip, agente) = ObtenerDatosCliente();
            var resultado = await _servicio.VerificarDosFactoresAsync(dto, ip, agente);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPost("registrar-cliente")]
        [AllowAnonymous]
        [EnableRateLimiting("registro")]
        public async Task<ActionResult<ResultadoOperacion<RegistroRespuestaDto>>> RegistrarCliente(
            [FromBody] RegistroClienteDto dto)
        {
            // Sin captcha visible aquí a pedido del negocio (registro sin fricción,
            // como la mayoría de tiendas). La protección anti-bot es honeypot +
            // rate limiting ("registro") + unicidad de correo/documento.
            var (ip, agente) = ObtenerDatosCliente();
            var resultado = await _servicio.RegistrarClienteAsync(dto!, ip, agente);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPost("registrar-vendedor")]
        [AllowAnonymous]
        [EnableRateLimiting("registro")]
        public async Task<ActionResult<ResultadoOperacion<RegistroRespuestaDto>>> RegistrarVendedor(
            [FromBody] RegistroVendedorDto dto)
        {
            // Sin captcha visible aquí a pedido del negocio (ver RegistrarCliente).
            var (ip, agente) = ObtenerDatosCliente();
            var resultado = await _servicio.RegistrarVendedorAsync(dto!, ip, agente);
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
        /// <summary>
        /// Envía un código de 6 dígitos. La respuesta no indica si el correo tiene cuenta.
        /// </summary>
        [HttpPost("codigo/solicitar")]
        [AllowAnonymous]
        [EnableRateLimiting("codigos")]
        public async Task<ActionResult<ResultadoOperacion<string>>> SolicitarCodigo(
            [FromBody] SolicitarCodigoAccesoDto dto)
        {
            var (ip, _) = ObtenerDatosCliente();
            var resultado = await _servicio.SolicitarCodigoAccesoAsync(dto?.Correo ?? string.Empty, ip);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPost("codigo/verificar")]
        [AllowAnonymous]
        [EnableRateLimiting("codigos")]
        public async Task<ActionResult<ResultadoOperacion<LoginRespuestaDto>>> VerificarCodigo(
            [FromBody] VerificarCodigoAccesoDto dto)
        {
            var (ip, agente) = ObtenerDatosCliente();
            var resultado = await _servicio.VerificarCodigoAccesoAsync(
                dto?.Correo ?? string.Empty, dto?.Codigo ?? string.Empty, ip, agente);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPost("codigo/completar-registro")]
        [AllowAnonymous]
        [EnableRateLimiting("codigos")]
        public async Task<ActionResult<ResultadoOperacion<LoginRespuestaDto>>> CompletarRegistroCodigo(
            [FromBody] CompletarRegistroCodigoDto dto)
        {
            var (ip, agente) = ObtenerDatosCliente();
            var resultado = await _servicio.CompletarRegistroCodigoAsync(dto, ip, agente);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        /// <summary>
        /// Compra sin cuenta. Si el correo es nuevo, abre sesión de cliente.
        /// Si ya existe, exige el código de un solo uso antes de entrar a esa cuenta.
        /// </summary>
        [HttpPost("compra-invitado")]
        [AllowAnonymous]
        [EnableRateLimiting("codigos")]
        public async Task<ActionResult<ResultadoOperacion<LoginRespuestaDto>>> PrepararCompraInvitado(
            [FromBody] PrepararCompraInvitadoDto dto)
        {
            // Sin captcha visible aquí a pedido del negocio (ver RegistrarCliente).
            var (ip, agente) = ObtenerDatosCliente();
            var resultado = await _servicio.PrepararCompraInvitadoAsync(dto!, ip, agente);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPost("recuperar-clave")]
        [AllowAnonymous]
        [EnableRateLimiting("codigos")]
        public async Task<ActionResult<ResultadoOperacion<string>>> RecuperarClave(
            [FromBody] RecuperarClaveDto dto)
        {
            var (ip, _) = ObtenerDatosCliente();
            var resultado = await _servicio.RecuperarClaveAsync(dto?.Correo ?? string.Empty, ip);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPost("recuperar-clave/verificar")]
        [AllowAnonymous]
        [EnableRateLimiting("codigos")]
        public async Task<ActionResult<ResultadoOperacion<string>>> VerificarRecuperacionClave(
            [FromBody] VerificarRecuperacionClaveDto dto)
        {
            var resultado = await _servicio.VerificarRecuperacionClaveAsync(
                dto?.Correo ?? string.Empty, dto?.Codigo ?? string.Empty);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPost("recuperar-clave/confirmar")]
        [AllowAnonymous]
        [EnableRateLimiting("codigos")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> ConfirmarRecuperacionClave(
            [FromBody] ConfirmarRecuperacionClaveDto dto)
        {
            var resultado = await _servicio.ConfirmarRecuperacionClaveAsync(
                dto?.Correo ?? string.Empty, dto?.Ticket ?? string.Empty, dto?.ContrasenaNueva ?? string.Empty);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        /// <summary>
        /// Primer cambio de contraseña tras el registro (sin iniciar sesión).
        /// </summary>
        [HttpPost("establecer-contrasena-inicial")]
        [AllowAnonymous]
        [EnableRateLimiting("codigos")]
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
            if (!Guid.TryParse(usuarioIdStr, out var usuarioId) || usuarioId == Guid.Empty)
                return Unauthorized();

            var resultado = await _servicio.CambiarPasswordAsync(
                usuarioId,
                dto.ContrasenaActual,
                dto.ContrasenaNueva);

            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        [HttpPost("refrescar")]
        [AllowAnonymous]
        [EnableRateLimiting("refrescar")]
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
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            var resultado = await _servicio.LogoutAsync(usuarioId.Value, dto.RefreshToken);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }

        private (string? Ip, string? Agente) ObtenerDatosCliente()
        {
            var http = _httpContextAccessor.HttpContext;
            var ip = http?.Connection.RemoteIpAddress?.ToString();
            var agente = http?.Request.Headers["User-Agent"].ToString();
            return (ip, agente);
        }

        private Guid? ObtenerUsuarioId()
        {
            var usuarioIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(usuarioIdStr, out var usuarioId) && usuarioId != Guid.Empty ? usuarioId : null;
        }
    }
}
