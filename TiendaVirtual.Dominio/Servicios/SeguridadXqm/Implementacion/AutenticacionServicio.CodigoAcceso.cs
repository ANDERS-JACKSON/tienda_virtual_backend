using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Extensiones.SeguridadXqm;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;
using TiendaVirtual.Dominio.Utilidad;
using TiendaVirtual.Dominio.Servicios.SoporteXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;

namespace TiendaVirtual.Dominio.Servicios.SeguridadXqm.Implementacion
{
    public partial class AutenticacionServicio
    {
        private const int OTP_DIGITOS = 6;
        private const int OTP_MINUTOS = 10;
        private const int OTP_MAX_INTENTOS = 5;
        private const int OTP_MAX_POR_CORREO_HORA = 5;
        private const int OTP_MAX_POR_IP_HORA = 20;
        private const int OTP_ESPERA_SEGUNDOS = 60;
        private const string PROPOSITO_ACCESO = "acceso";
        private const string PROPOSITO_RECUPERACION = "recuperacion";
        private const int RECUPERACION_DIGITOS = 6;
        private const int RECUPERACION_MINUTOS = 5;

        public async Task<ResultadoOperacion<string>> SolicitarCodigoAccesoAsync(
            string correo, string? direccionIp)
        {
            try
            {
                var correoNorm = NormalizarCorreo(correo);
                if (correoNorm == null)
                    return ResultadoOperacion<string>.SetError("Ingresa un correo electrónico válido.");

                var limite = await ValidarLimiteCodigosAsync(correoNorm, direccionIp);
                if (!limite.Exito)
                    return ResultadoOperacion<string>.SetError(limite.Mensaje);

                var codigo = GenerarCodigoNumerico(OTP_DIGITOS);
                var enviado = await EnviarCodigoPorCorreoAsync(correoNorm, codigo);
                if (!enviado)
                    return ResultadoOperacion<string>.SetError(
                        "No pudimos enviar el código. Intenta de nuevo en unos minutos.");

                await GuardarCodigoAsync(correoNorm, codigo, direccionIp, PROPOSITO_ACCESO, OTP_MINUTOS);

                return ResultadoOperacion<string>.SetExito(
                    "Te enviamos un código de 6 dígitos. Revisa también la carpeta de spam. Vence en 10 minutos.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al solicitar código de acceso para {Correo}", correo);
                return ResultadoOperacion<string>.SetError(
                    "No pudimos enviar el código. Intenta de nuevo en unos minutos.");
            }
        }

        public async Task<ResultadoOperacion<LoginRespuestaDto>> VerificarCodigoAccesoAsync(
            string correo, string codigo, string? direccionIp, string? agenteUsuario)
        {
            try
            {
                var correoNorm = NormalizarCorreo(correo);
                var codigoNorm = (codigo ?? string.Empty).Trim();
                if (correoNorm == null || codigoNorm.Length != OTP_DIGITOS || codigoNorm.Any(c => !char.IsDigit(c)))
                    return ResultadoOperacion<LoginRespuestaDto>.SetError(
                        "Código incorrecto o vencido. Solicita uno nuevo.");

                var registro = await _context.CodigosAccesoCorreo
                    .Where(c => c.Correo == correoNorm && !c.Usado && c.Proposito == PROPOSITO_ACCESO)
                    .OrderByDescending(c => c.FechaCreacion)
                    .FirstOrDefaultAsync();

                var hashReferencia = registro?.CodigoHash ?? HashearCodigo(correoNorm, "000000");
                var coincide = CodigoCoincide(hashReferencia, correoNorm, codigoNorm);
                var vencido = registro == null
                    || registro.ExpiraEn < DateTime.UtcNow
                    || registro.Intentos >= OTP_MAX_INTENTOS;

                if (!coincide || vencido)
                {
                    if (registro != null)
                    {
                        registro.Intentos++;
                        if (registro.Intentos >= OTP_MAX_INTENTOS || registro.ExpiraEn < DateTime.UtcNow)
                            registro.Usado = true;
                        await _context.SaveChangesAsync();
                    }

                    var agotado = registro != null && registro.Usado && registro.Intentos >= OTP_MAX_INTENTOS;
                    return ResultadoOperacion<LoginRespuestaDto>.SetError(
                        agotado
                            ? "Demasiados intentos. Solicita un código nuevo."
                            : "Código incorrecto o vencido. Solicita uno nuevo.");
                }

                registro!.Usado = true;
                await _context.SaveChangesAsync();

                var usuario = await QueryUsuarioConRelaciones()
                    .FirstOrDefaultAsync(u => u.Correo == correoNorm);

                if (usuario == null)
                {
                    return ResultadoOperacion<LoginRespuestaDto>.SetExito(new LoginRespuestaDto
                    {
                        RequiereCompletarRegistro = true,
                        TicketRegistro = _jwtService.GenerarTicketRegistroCorreo(correoNorm),
                        RegistroPendiente = new RegistroGooglePendienteDto { Correo = correoNorm },
                        Mensaje = "Confirma tus datos para crear la cuenta."
                    });
                }

                usuario.CorreoConfirmado = true;

                // A5: si la cuenta era de invitado (o no tenía el correo confirmado),
                // ya probó ser dueña del correo con este código → deja de ser invitada
                // y se revocan las sesiones viejas (p. ej. la de invitado de corta vida).
                if (usuario.EsInvitado)
                {
                    usuario.EsInvitado = false;
                    await RevocarTokensRefrescoAsync(usuario.UsuarioId);
                }
                await _context.SaveChangesAsync();

                return await ProcesarAccesoUsuarioAsync(
                    usuario, direccionIp, agenteUsuario, esLoginExterno: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar código de acceso");
                return ResultadoOperacion<LoginRespuestaDto>.SetError(
                    "Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        public async Task<ResultadoOperacion<LoginRespuestaDto>> CompletarRegistroCodigoAsync(
            CompletarRegistroCodigoDto dto, string? direccionIp, string? agenteUsuario)
        {
            using var transaccion = await _context.Database.BeginTransactionAsync();
            try
            {
                if (dto == null)
                    return ResultadoOperacion<LoginRespuestaDto>.SetError("Datos inválidos.");

                var terminos = ValidarAceptacionTerminos(dto.AceptaTerminos);
                if (!terminos.Exito)
                    return ResultadoOperacion<LoginRespuestaDto>.SetError(terminos.Mensaje);

                var correo = _jwtService.LeerCorreoTicketRegistro(dto.Ticket);
                if (correo == null)
                    return ResultadoOperacion<LoginRespuestaDto>.SetError(
                        "El código de registro expiró. Solicita uno nuevo.");

                var existente = await QueryUsuarioConRelaciones()
                    .FirstOrDefaultAsync(u => u.Correo == correo);
                if (existente != null)
                {
                    existente.CorreoConfirmado = true;
                    var acceso = await ProcesarAccesoUsuarioAsync(
                        existente, direccionIp, agenteUsuario, esLoginExterno: true);
                    if (!acceso.Exito)
                        return acceso;
                    await transaccion.CommitAsync();
                    return acceso;
                }

                var errorTelefono = NormalizarTelefonoOpcional(dto.Persona.Telefono, out var telefono);
                if (errorTelefono != null)
                    return ResultadoOperacion<LoginRespuestaDto>.SetError(errorTelefono);
                dto.Persona.Telefono = telefono;
                dto.Persona.CorreoElectronico = correo;

                var validacionComun = await ValidarRegistroComunAsync(correo, dto.Persona);
                if (!validacionComun.Exito)
                    return ResultadoOperacion<LoginRespuestaDto>.SetError(validacionComun.Mensaje);

                var persona = dto.Persona.ToEntidad();
                persona.CorreoElectronico = correo;
                _context.Personas.Add(persona);
                await _context.SaveChangesAsync();

                var usuario = CrearUsuario(
                    persona.PersonaId, correo, GenerarClaveAleatoria(32), forzarCambioClave: false);
                usuario.CorreoConfirmado = true;
                usuario.EsInvitado = false;
                usuario.FechaAceptacionTerminos = DateTime.UtcNow;
                usuario.VersionPoliticas = PoliticasConsentimiento.VersionVigente;
                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                _context.UsuarioRoles.Add(new UsuarioRol
                {
                    UsuarioId = usuario.UsuarioId,
                    RolId = (short)TipoRol.Cliente
                });
                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                var creado = await QueryUsuarioConRelaciones()
                    .FirstAsync(u => u.UsuarioId == usuario.UsuarioId);

                return await ProcesarAccesoUsuarioAsync(
                    creado, direccionIp, agenteUsuario, esLoginExterno: true);
            }
            catch (Exception ex)
            {
                await transaccion.RollbackAsync();
                _logger.LogError(ex, "Error al completar registro con código de correo");
                return ResultadoOperacion<LoginRespuestaDto>.SetError(
                    "Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        public async Task<ResultadoOperacion<LoginRespuestaDto>> PrepararCompraInvitadoAsync(
            PrepararCompraInvitadoDto dto, string? direccionIp, string? agenteUsuario)
        {
            try
            {
                if (dto == null)
                    return ResultadoOperacion<LoginRespuestaDto>.SetError("Datos inválidos.");

                // Honeypot anti-bot: no hay captcha visible aquí a pedido del negocio
                // (ver AutenticacionServicio.RegistrarClienteAsync).
                if (!string.IsNullOrWhiteSpace(dto.Sitio))
                    return ResultadoOperacion<LoginRespuestaDto>.SetError("Datos inválidos.");

                var terminos = ValidarAceptacionTerminos(dto.AceptaTerminos);
                if (!terminos.Exito)
                    return ResultadoOperacion<LoginRespuestaDto>.SetError(terminos.Mensaje);

                var correo = NormalizarCorreo(dto.Correo);
                if (correo == null)
                    return ResultadoOperacion<LoginRespuestaDto>.SetError("Ingresa un correo electrónico válido.");

                var yaExiste = await _context.Usuarios.AnyAsync(u => u.Correo == correo);
                if (yaExiste)
                {
                    var envio = await SolicitarCodigoAccesoAsync(correo, direccionIp);
                    if (!envio.Exito)
                        return ResultadoOperacion<LoginRespuestaDto>.SetError(envio.Mensaje);

                    return ResultadoOperacion<LoginRespuestaDto>.SetExito(new LoginRespuestaDto
                    {
                        RequiereCodigo = true,
                        Mensaje = "Este correo ya está registrado. Te enviamos un código de 6 dígitos para continuar la compra con tu cuenta."
                    });
                }

                return await CrearCuentaInvitadoAsync(dto, correo, direccionIp, agenteUsuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al preparar compra como invitado");
                return ResultadoOperacion<LoginRespuestaDto>.SetError(
                    "Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        private async Task<ResultadoOperacion<LoginRespuestaDto>> CrearCuentaInvitadoAsync(
            PrepararCompraInvitadoDto dto, string correo, string? direccionIp, string? agenteUsuario)
        {
            using var transaccion = await _context.Database.BeginTransactionAsync();
            try
            {
                var errorTelefono = NormalizarTelefonoObligatorio(dto.Persona.Telefono, out var telefono);
                if (errorTelefono != null)
                    return ResultadoOperacion<LoginRespuestaDto>.SetError(errorTelefono);

                dto.Persona.Telefono = telefono;
                dto.Persona.CorreoElectronico = correo;

                var validacionComun = await ValidarRegistroComunAsync(correo, dto.Persona);
                if (!validacionComun.Exito)
                    return ResultadoOperacion<LoginRespuestaDto>.SetError(validacionComun.Mensaje);

                var persona = dto.Persona.ToEntidad();
                persona.CorreoElectronico = correo;
                persona.Telefono = telefono;
                _context.Personas.Add(persona);
                await _context.SaveChangesAsync();

                var usuario = CrearUsuario(
                    persona.PersonaId, correo, GenerarClaveAleatoria(32), forzarCambioClave: false);
                usuario.EsInvitado = true;
                usuario.CorreoConfirmado = false;
                usuario.FechaAceptacionTerminos = DateTime.UtcNow;
                usuario.VersionPoliticas = PoliticasConsentimiento.VersionVigente;
                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                _context.UsuarioRoles.Add(new UsuarioRol
                {
                    UsuarioId = usuario.UsuarioId,
                    RolId = (short)TipoRol.Cliente
                });
                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                var creado = await QueryUsuarioConRelaciones()
                    .FirstAsync(u => u.UsuarioId == usuario.UsuarioId);

                return await ProcesarAccesoUsuarioAsync(
                    creado, direccionIp, agenteUsuario, esLoginExterno: true);
            }
            catch (Exception ex)
            {
                await transaccion.RollbackAsync();
                _logger.LogError(ex, "Error al crear cuenta de compra como invitado");
                return ResultadoOperacion<LoginRespuestaDto>.SetError(
                    "Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        private async Task<ResultadoOperacion<bool>> ValidarLimiteCodigosAsync(string correo, string? direccionIp)
        {
            var desde = DateTime.UtcNow.AddHours(-1);
            var enviosCorreo = await _context.CodigosAccesoCorreo
                .CountAsync(c => c.Correo == correo && c.FechaCreacion >= desde);

            if (enviosCorreo >= OTP_MAX_POR_CORREO_HORA)
                return ResultadoOperacion<bool>.SetError(
                    "Pediste demasiados códigos. Espera una hora e intenta de nuevo.");

            var ultimo = await _context.CodigosAccesoCorreo
                .Where(c => c.Correo == correo)
                .OrderByDescending(c => c.FechaCreacion)
                .Select(c => (DateTime?)c.FechaCreacion)
                .FirstOrDefaultAsync();

            if (ultimo.HasValue && ultimo.Value > DateTime.UtcNow.AddSeconds(-OTP_ESPERA_SEGUNDOS))
                return ResultadoOperacion<bool>.SetError(
                    "Espera un minuto antes de solicitar otro código.");

            if (!string.IsNullOrWhiteSpace(direccionIp))
            {
                var enviosIp = await _context.CodigosAccesoCorreo
                    .CountAsync(c => c.DireccionIp == direccionIp && c.FechaCreacion >= desde);
                if (enviosIp >= OTP_MAX_POR_IP_HORA)
                    return ResultadoOperacion<bool>.SetError(
                        "Hay demasiados intentos desde esta red. Espera un momento.");
            }

            return ResultadoOperacion<bool>.SetExito(true);
        }

        private async Task GuardarCodigoAsync(
            string correo, string codigo, string? direccionIp, string proposito, int minutos)
        {
            var limite = DateTime.UtcNow.AddDays(-2);
            await _context.CodigosAccesoCorreo
                .Where(c => c.FechaCreacion < limite)
                .ExecuteDeleteAsync();

            var previos = await _context.CodigosAccesoCorreo
                .Where(c => c.Correo == correo && c.Proposito == proposito && !c.Usado)
                .ToListAsync();
            foreach (var previo in previos)
                previo.Usado = true;

            _context.CodigosAccesoCorreo.Add(new CodigoAccesoCorreo
            {
                CodigoAccesoId = Guid.NewGuid(),
                Correo = correo,
                Proposito = proposito,
                CodigoHash = proposito == PROPOSITO_ACCESO
                    ? HashearCodigo(correo, codigo)
                    : HashearSecreto(correo, proposito, codigo),
                ExpiraEn = DateTime.UtcNow.AddMinutes(minutos),
                Intentos = 0,
                Usado = false,
                FechaCreacion = DateTime.UtcNow,
                DireccionIp = string.IsNullOrWhiteSpace(direccionIp) ? null : direccionIp
            });

            await _context.SaveChangesAsync();
        }

        private async Task<bool> EnviarCodigoPorCorreoAsync(string correo, string codigo)
        {
            var cuerpo = PlantillasCorreoDefault.Envolver($@"
            <p style=""margin:0 0 8px;text-align:center;"">Tu código para entrar es</p>
            <p style=""margin:0 0 16px;text-align:center;font-size:32px;letter-spacing:0.35em;font-weight:700;"">{WebUtility.HtmlEncode(codigo)}</p>
            <p style=""margin:0;text-align:center;font-size:13px;color:#555555;"">
              Vence en 10 minutos y solo puede usarse una vez.
              Si no pediste este código, ignora este mensaje.
            </p>");

            return await _email.EnviarHtmlAsync(
                correo,
                correo,
                "Tu código de acceso",
                cuerpo);
        }

        private string HashearSecreto(string correo, string proposito, string valor)
        {
            var pepper = _configuration["Jwt:Key"] ?? string.Empty;
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(pepper));
            var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{proposito}:{correo}:{valor}"));
            return Convert.ToBase64String(bytes);
        }

        private bool SecretoCoincide(string hashGuardado, string correo, string proposito, string valor)
        {
            try
            {
                var calculado = Convert.FromBase64String(HashearSecreto(correo, proposito, valor));
                var guardado = Convert.FromBase64String(hashGuardado);
                if (calculado.Length != guardado.Length)
                    return false;
                return CryptographicOperations.FixedTimeEquals(calculado, guardado);
            }
            catch
            {
                return false;
            }
        }

        private string HashearCodigo(string correo, string codigo)
        {
            var pepper = _configuration["Jwt:Key"] ?? string.Empty;
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(pepper));
            var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{correo}:{codigo}"));
            return Convert.ToBase64String(bytes);
        }

        private bool CodigoCoincide(string hashGuardado, string correo, string codigo)
        {
            try
            {
                var calculado = Convert.FromBase64String(HashearCodigo(correo, codigo));
                var guardado = Convert.FromBase64String(hashGuardado);
                if (calculado.Length != guardado.Length)
                    return false;
                return CryptographicOperations.FixedTimeEquals(calculado, guardado);
            }
            catch
            {
                return false;
            }
        }

        private static string GenerarCodigoNumerico(int digitos)
        {
            return RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, digitos))
                .ToString(new string('0', digitos));
        }

        private static string? NormalizarCorreo(string? correo)
        {
            var valor = correo?.Trim().ToLowerInvariant() ?? string.Empty;
            if (valor.Length is < 6 or > 254)
                return null;
            if (!valor.Contains('@') || valor.StartsWith('@') || valor.EndsWith('@'))
                return null;
            return valor;
        }

        private static string? NormalizarTelefonoObligatorio(string? telefono, out string normalizado)
        {
            normalizado = SoloDigitosTelefono(telefono);
            if (normalizado.Length != 9 || normalizado[0] != '9')
                return "Ingresa un celular de 9 dígitos que empiece con 9.";
            return null;
        }

        private static string? NormalizarTelefonoOpcional(string? telefono, out string? normalizado)
        {
            if (string.IsNullOrWhiteSpace(telefono))
            {
                normalizado = null;
                return null;
            }

            var error = NormalizarTelefonoObligatorio(telefono, out var valor);
            normalizado = error == null ? valor : null;
            return error;
        }

        private static string SoloDigitosTelefono(string? telefono)
        {
            var digits = new string((telefono ?? string.Empty).Where(char.IsDigit).ToArray());
            if (digits.StartsWith("51") && digits.Length == 11)
                digits = digits[2..];
            return digits;
        }
    }
}
