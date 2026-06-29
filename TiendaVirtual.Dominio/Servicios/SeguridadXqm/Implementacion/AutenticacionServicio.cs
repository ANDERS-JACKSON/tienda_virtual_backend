using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Extensiones.SeguridadXqm;
using TiendaVirtual.Dominio.Modelo.PagoXqm;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Dominio.Servicios.Sistema;
using TiendaVirtual.Dominio.Servicios.Sistema.Implementacion;
using TiendaVirtual.Dominio.Servicios.SoporteXqm;
using TiendaVirtual.Dominio.Utilidad;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;

namespace TiendaVirtual.Dominio.Servicios.SeguridadXqm.Implementacion
{
    public partial class AutenticacionServicio : IAutenticacionServicio
    {
        protected readonly TiendaVirtualDbContext _context;
        protected readonly JwtTokenService _jwtService;
        protected readonly ITwoFactorService _twoFactorService;
        protected readonly IConfiguration _configuration;
        protected readonly INotificacionServicio _notificacionServicio;
        protected readonly IGoogleAuthServicio _googleAuth;

        private const int DURACION_TOKEN_MINUTOS = 60;
        private const int DURACION_REFRESH_DIAS = 30;
        private const int LONGITUD_CLAVE_AUTO = 10;

        // Roles que exigen 2FA obligatorio
        private static readonly string[] ROLES_CON_2FA = { "ADMIN", "VERIFICADOR" };

        public AutenticacionServicio(
            TiendaVirtualDbContext context,
            JwtTokenService jwtService,
            ITwoFactorService twoFactorService,
            IConfiguration configuration,
            INotificacionServicio notificacionServicio,
            IGoogleAuthServicio googleAuth)
        {
            _context = context;
            _jwtService = jwtService;
            _twoFactorService = twoFactorService;
            _configuration = configuration;
            _notificacionServicio = notificacionServicio;
            _googleAuth = googleAuth;
        }

        // LOGIN (paso 1)
        public async Task<ResultadoOperacion<LoginRespuestaDto>> LoginAsync(
            LoginDto dto, string? direccionIp, string? agenteUsuario)
        {
            try
            {
                if (dto == null)
                    return ResultadoOperacion<LoginRespuestaDto>.SetError("El DTO es nulo.");

                var usuario = await _context.Usuarios
                    .Include(u => u.Persona)
                    .Include(u => u.UsuarioRoles).ThenInclude(ur => ur.Rol)
                    .Include(u => u.Vendedor)
                    .FirstOrDefaultAsync(u => u.Correo == dto.Correo.ToLower());

                if (usuario == null)
                    return ResultadoOperacion<LoginRespuestaDto>.SetError("Correo o contraseña incorrectos.");

                if (!BCrypt.Net.BCrypt.Verify(dto.Contrasena, usuario.Contrasena))
                    return ResultadoOperacion<LoginRespuestaDto>.SetError("Correo o contraseña incorrectos.");

                return await ProcesarAccesoUsuarioAsync(usuario, direccionIp, agenteUsuario);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<LoginRespuestaDto>.SetError("Error al iniciar sesión: " + ex.Message);
            }
        }

        // LOGIN (paso 2: validar código TOTP)
        public async Task<ResultadoOperacion<TokenRespuestaDto>> VerificarDosFactoresAsync(
            Verificar2FaRequestDto dto, string? direccionIp, string? agenteUsuario)
        {
            try
            {
                if (dto == null)
                    return ResultadoOperacion<TokenRespuestaDto>.SetError("El DTO es nulo.");

                var principal = _jwtService.ValidarToken(dto.TokenTemporal);
                if (principal == null)
                    return ResultadoOperacion<TokenRespuestaDto>.SetError(
                        "El token temporal es inválido o expiró. Inicia sesión de nuevo.");

                if (principal.FindFirst("scope")?.Value != "2fa_pending")
                    return ResultadoOperacion<TokenRespuestaDto>.SetError(
                        "Token no autorizado para esta operación.");

                var usuarioIdStr = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(usuarioIdStr, out int usuarioId))
                    return ResultadoOperacion<TokenRespuestaDto>.SetError("Token inválido.");

                var clave = _configuration["TwoFactor:ClaveCifrado"]!;
                var secretoPendienteCifrado = principal.FindFirst("setupSecret")?.Value;
                bool esPrimerLogin = !string.IsNullOrEmpty(secretoPendienteCifrado);

                var usuario = await _context.Usuarios
                    .Include(u => u.Persona)
                    .Include(u => u.UsuarioRoles).ThenInclude(ur => ur.Rol)
                    .Include(u => u.Vendedor)
                    .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);

                if (usuario == null)
                    return ResultadoOperacion<TokenRespuestaDto>.SetError("Usuario no encontrado.");

                if (usuario.Estado != TipoEstadoUsuario.Activo)
                    return ResultadoOperacion<TokenRespuestaDto>.SetError("Tu cuenta no está activa.");

                // Re-verificar que efectivamente requiere 2FA (defensa contra escalada de privilegios)
                var roles = usuario.UsuarioRoles.Select(ur => ur.Rol.Nombre).ToList();
                if (!roles.Any(r => ROLES_CON_2FA.Contains(r)))
                    return ResultadoOperacion<TokenRespuestaDto>.SetError(
                        "Este usuario no requiere autenticación de dos factores.");

                string secretoPlano;
                if (esPrimerLogin)
                {
                    secretoPlano = _twoFactorService.DescifrarSecreto(secretoPendienteCifrado!, clave);
                }
                else
                {
                    if (!usuario.TwoFactorEnabled || string.IsNullOrEmpty(usuario.TwoFactorSecret))
                        return ResultadoOperacion<TokenRespuestaDto>.SetError(
                            "El usuario no tiene 2FA configurado.");

                    secretoPlano = _twoFactorService.DescifrarSecreto(usuario.TwoFactorSecret, clave);
                }

                if (!_twoFactorService.ValidarCodigo(secretoPlano, dto.Codigo))
                    return ResultadoOperacion<TokenRespuestaDto>.SetError(
                        "Código de verificación incorrecto.");

                // Primer login válido → AHORA sí persistimos el secreto cifrado
                if (esPrimerLogin)
                {
                    usuario.TwoFactorSecret = secretoPendienteCifrado;
                    usuario.TwoFactorEnabled = true;
                }

                usuario.UltimoAcceso = DateTime.UtcNow;
                var respuesta = await EmitirTokensAsync(usuario, direccionIp, agenteUsuario);
                await _context.SaveChangesAsync();

                return ResultadoOperacion<TokenRespuestaDto>.SetExito(respuesta);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<TokenRespuestaDto>.SetError(
                    "Error al verificar el código: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // REGISTRAR CLIENTE
        // ─────────────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<RegistroRespuestaDto>> RegistrarClienteAsync(
            RegistroClienteDto dto, string? direccionIp, string? agenteUsuario)
        {
            using var transaccion = await _context.Database.BeginTransactionAsync();
            try
            {
                if (dto == null)
                    return ResultadoOperacion<RegistroRespuestaDto>.SetError("El DTO es nulo.");

                var validacionComun = await ValidarRegistroComunAsync(dto.Correo, dto.Persona);
                if (!validacionComun.Exito)
                    return ResultadoOperacion<RegistroRespuestaDto>.SetError(validacionComun.Mensaje);

                var persona = dto.Persona.ToEntidad();
                _context.Personas.Add(persona);
                await _context.SaveChangesAsync();

                // Clave auto generada: el usuario la recibirá por correo
                var claveAuto = GenerarClaveAleatoria(LONGITUD_CLAVE_AUTO);

                var usuario = CrearUsuario(persona.PersonaId, dto.Correo, claveAuto, forzarCambioClave: true);
                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                _context.UsuarioRoles.Add(new UsuarioRol
                {
                    UsuarioId = usuario.UsuarioId,
                    RolId = (short)TipoRol.Cliente
                });
                await _context.SaveChangesAsync();

                await transaccion.CommitAsync();

                // Envío de correo fuera de la transacción para no demorar el commit
                var nombre = $"{persona.Nombres} {persona.ApellidoPaterno}".Trim();
                var correoEnviado = await IntentarEnviarCreacionAsync(usuario.UsuarioId, dto.Correo, claveAuto);

                return ResultadoOperacion<RegistroRespuestaDto>.SetExito(new RegistroRespuestaDto
                {
                    UsuarioId = usuario.UsuarioId,
                    Correo = usuario.Correo,
                    NombreCompleto = nombre,
                    CorreoEnviado = correoEnviado,
                    Mensaje = correoEnviado
                        ? "Tu cuenta fue creada. Te enviamos la contraseña a tu correo electrónico."
                        : "Tu cuenta fue creada, pero no pudimos enviar el correo con la contraseña. Contacta al soporte."
                });
            }
            catch (Exception ex)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion<RegistroRespuestaDto>.SetError("Error al registrar cliente: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // REGISTRAR VENDEDOR
        // ─────────────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<RegistroRespuestaDto>> RegistrarVendedorAsync(
            RegistroVendedorDto dto, string? direccionIp, string? agenteUsuario)
        {
            using var transaccion = await _context.Database.BeginTransactionAsync();
            try
            {
                if (dto == null)
                    return ResultadoOperacion<RegistroRespuestaDto>.SetError("El DTO es nulo.");

                var validacionComun = await ValidarRegistroComunAsync(dto.Correo, dto.Persona);
                if (!validacionComun.Exito)
                    return ResultadoOperacion<RegistroRespuestaDto>.SetError(validacionComun.Mensaje);

                // Slug único
                var slug = dto.SlugTienda.ToLower();
                if (await _context.Vendedores.AnyAsync(v => v.SlugTienda == slug))
                    return ResultadoOperacion<RegistroRespuestaDto>.SetError("Ese nombre de tienda ya está en uso, prueba con otro.");

                var persona = dto.Persona.ToEntidad();
                _context.Personas.Add(persona);
                await _context.SaveChangesAsync();

                var claveAuto = GenerarClaveAleatoria(LONGITUD_CLAVE_AUTO);

                var usuario = CrearUsuario(persona.PersonaId, dto.Correo, claveAuto, forzarCambioClave: true);
                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                // Vendedor inicia en PENDIENTE_VERIFICACION hasta que el verificador apruebe
                var vendedor = new Vendedor
                {
                    UsuarioId = usuario.UsuarioId,
                    NombreTienda = dto.NombreTienda,
                    SlugTienda = slug,
                    Biografia = dto.Biografia,
                    NumeroYape = dto.NumeroYape,
                    Estado = TipoEstadoVendedor.PendienteVerificacion,
                    CalificacionPromedio = 0,
                    TotalVentas = 0,
                    VendePatrones = false
                };
                _context.Vendedores.Add(vendedor);
                await _context.SaveChangesAsync();

                // Crear billetera vacía para recibir pagos
                _context.Billeteras.Add(new Billetera
                {
                    VendedorId = vendedor.VendedorId,
                    SaldoDisponible = 0,
                    SaldoPendiente = 0,
                    TotalGanado = 0,
                    TotalRetirado = 0
                });

                // Asignar ambos roles: vendedor + cliente (puede comprar también)
                _context.UsuarioRoles.AddRange(
                    new UsuarioRol { UsuarioId = usuario.UsuarioId, RolId = (short)TipoRol.Cliente },
                    new UsuarioRol { UsuarioId = usuario.UsuarioId, RolId = (short)TipoRol.Vendedor }
                );
                await _context.SaveChangesAsync();

                await transaccion.CommitAsync();

                var nombre = $"{persona.Nombres} {persona.ApellidoPaterno}".Trim();
                var correoEnviado = await IntentarEnviarCreacionAsync(usuario.UsuarioId, dto.Correo, claveAuto);

                return ResultadoOperacion<RegistroRespuestaDto>.SetExito(new RegistroRespuestaDto
                {
                    UsuarioId = usuario.UsuarioId,
                    Correo = usuario.Correo,
                    NombreCompleto = nombre,
                    CorreoEnviado = correoEnviado,
                    Mensaje = correoEnviado
                        ? "Tu tienda fue creada. Te enviamos la contraseña a tu correo electrónico."
                        : "Tu tienda fue creada, pero no pudimos enviar el correo con la contraseña. Contacta al soporte."
                });
            }
            catch (Exception ex)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion<RegistroRespuestaDto>.SetError("Error al registrar vendedor: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // REGISTRAR ADMINISTRADOR
        // ─────────────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<RegistroRespuestaDto>> RegistrarAdministradorAsync(
            RegistroAdministradorDto dto, string? direccionIp, string? agenteUsuario)
        {
            using var transaccion = await _context.Database.BeginTransactionAsync();
            try
            {
                if (dto == null)
                    return ResultadoOperacion<RegistroRespuestaDto>.SetError("El DTO es nulo.");

                var validacionComun = await ValidarRegistroComunAsync(dto.Correo, dto.Persona);
                if (!validacionComun.Exito)
                    return ResultadoOperacion<RegistroRespuestaDto>.SetError(validacionComun.Mensaje);

                var persona = dto.Persona.ToEntidad();
                _context.Personas.Add(persona);
                await _context.SaveChangesAsync();

                var claveAuto = GenerarClaveAleatoria(LONGITUD_CLAVE_AUTO);

                var usuario = CrearUsuario(persona.PersonaId, dto.Correo, claveAuto, forzarCambioClave: true);
                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                _context.UsuarioRoles.Add(new UsuarioRol
                {
                    UsuarioId = usuario.UsuarioId,
                    RolId = (short)TipoRol.Admin
                });
                await _context.SaveChangesAsync();

                await transaccion.CommitAsync();

                var nombre = $"{persona.Nombres} {persona.ApellidoPaterno}".Trim();
                var correoEnviado = await IntentarEnviarCreacionAsync(usuario.UsuarioId, dto.Correo, claveAuto);

                return ResultadoOperacion<RegistroRespuestaDto>.SetExito(new RegistroRespuestaDto
                {
                    UsuarioId = usuario.UsuarioId,
                    Correo = usuario.Correo,
                    NombreCompleto = nombre,
                    CorreoEnviado = correoEnviado,
                    Mensaje = correoEnviado
                        ? "Administrador creado. Se envió la contraseña al correo."
                        : "Administrador creado, pero no pudimos enviar el correo con la contraseña."
                });
            }
            catch (Exception ex)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion<RegistroRespuestaDto>.SetError("Error al registrar administrador: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // RECUPERAR CLAVE (olvidé mi contraseña)
        // ─────────────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<string>> RecuperarClaveAsync(string correo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(correo))
                    return ResultadoOperacion<string>.SetError("El correo es obligatorio.");

                var correoNorm = correo.Trim().ToLower();

                var usuario = await _context.Usuarios
                    .Include(u => u.Persona)
                    .FirstOrDefaultAsync(u => u.Correo == correoNorm);

                // Mensaje genérico: NO revelamos si el correo está registrado
                // (defensa contra enumeración de usuarios).
                const string MENSAJE_OK =
                    "Si el correo está registrado, te enviamos una nueva contraseña en unos segundos.";

                if (usuario == null)
                    return ResultadoOperacion<string>.SetExito(MENSAJE_OK);

                if (usuario.Estado != TipoEstadoUsuario.Activo)
                    return ResultadoOperacion<string>.SetExito(MENSAJE_OK);

                var nuevaClave = GenerarClaveAleatoria(LONGITUD_CLAVE_AUTO);
                usuario.Contrasena = BCrypt.Net.BCrypt.HashPassword(nuevaClave);
                usuario.ForzarCambioClave = true;
                await _context.SaveChangesAsync();

                var nombre = usuario.Persona != null
                    ? $"{usuario.Persona.Nombres} {usuario.Persona.ApellidoPaterno}".Trim()
                    : usuario.Correo;

                await _notificacionServicio.CrearAsync(
                    usuario.UsuarioId,
                    TipoNotificacion.RecuperacionClaveSolicitada,
                    "Recuperación de contraseña",
                    "Recibimos tu solicitud para restablecer la contraseña.",
                    null,
                    plantillaEmail: PlantillaCorreo.RecuperacionClave,
                    placeholdersEmail: new Dictionary<string, string>
                    {
                        ["usuario"] = usuario.Correo,
                        ["clave"] = nuevaClave
                    });

                return ResultadoOperacion<string>.SetExito(MENSAJE_OK);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<string>.SetError(
                    "Error al recuperar contraseña: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // CAMBIAR CONTRASEÑA (usuario autenticado)
        // ─────────────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<bool>> CambiarPasswordAsync(
            int usuarioId, string contrasenaActual, string contrasenaNueva)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(contrasenaActual))
                    return ResultadoOperacion<bool>.SetError("La contraseña actual es requerida.");

                var validacionNueva = ContrasenaValidador.Validar(contrasenaNueva);
                if (!validacionNueva.Exito)
                    return ResultadoOperacion<bool>.SetError(validacionNueva.Mensaje);

                if (contrasenaActual == contrasenaNueva)
                    return ResultadoOperacion<bool>.SetError(
                        "La contraseña nueva debe ser diferente a la actual.");

                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);

                if (usuario == null)
                    return ResultadoOperacion<bool>.SetError("Usuario no encontrado.");

                if (!BCrypt.Net.BCrypt.Verify(contrasenaActual, usuario.Contrasena))
                    return ResultadoOperacion<bool>.SetError("La contraseña actual es incorrecta.");

                usuario.Contrasena = BCrypt.Net.BCrypt.HashPassword(contrasenaNueva);
                usuario.ForzarCambioClave = false;
                await _context.SaveChangesAsync();

                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.SetError(
                    "Error al cambiar la contraseña: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // ESTABLECER CONTRASEÑA INICIAL (sin sesión, tras registro)
        // ─────────────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<bool>> EstablecerContrasenaInicialAsync(
            string correo, string contrasenaActual, string contrasenaNueva)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(correo))
                    return ResultadoOperacion<bool>.SetError("El correo es obligatorio.");

                if (string.IsNullOrWhiteSpace(contrasenaActual))
                    return ResultadoOperacion<bool>.SetError("La contraseña del correo es obligatoria.");

                var validacionNueva = ContrasenaValidador.Validar(contrasenaNueva);
                if (!validacionNueva.Exito)
                    return ResultadoOperacion<bool>.SetError(validacionNueva.Mensaje);

                if (contrasenaActual == contrasenaNueva)
                    return ResultadoOperacion<bool>.SetError(
                        "La contraseña nueva debe ser diferente a la del correo.");

                var correoNorm = correo.Trim().ToLower();
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Correo == correoNorm);

                if (usuario == null)
                    return ResultadoOperacion<bool>.SetError("Correo o contraseña incorrectos.");

                if (!usuario.ForzarCambioClave)
                    return ResultadoOperacion<bool>.SetError(
                        "Esta cuenta ya tiene una contraseña definida. Inicia sesión y cámbiala desde tu perfil.");

                if (!BCrypt.Net.BCrypt.Verify(contrasenaActual, usuario.Contrasena))
                    return ResultadoOperacion<bool>.SetError("Correo o contraseña incorrectos.");

                usuario.Contrasena = BCrypt.Net.BCrypt.HashPassword(contrasenaNueva);
                usuario.ForzarCambioClave = false;
                await _context.SaveChangesAsync();

                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.SetError(
                    "Error al establecer la contraseña: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // REFRESCAR TOKEN
        // ─────────────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<TokenRespuestaDto>> RefrescarTokenAsync(
            string refreshToken, string? direccionIp, string? agenteUsuario)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(refreshToken))
                    return ResultadoOperacion<TokenRespuestaDto>.SetError("Refresh token requerido.");

                var hash = _jwtService.HashearRefreshToken(refreshToken);

                var tokenBd = await _context.TokensRefresco
                    .Include(t => t.Usuario).ThenInclude(u => u.Persona)
                    .Include(t => t.Usuario).ThenInclude(u => u.UsuarioRoles).ThenInclude(ur => ur.Rol)
                    .Include(t => t.Usuario).ThenInclude(u => u.Vendedor)
                    .FirstOrDefaultAsync(t => t.TokenHash == hash);

                if (tokenBd == null)
                    return ResultadoOperacion<TokenRespuestaDto>.SetError("Refresh token inválido.");

                if (tokenBd.Revocado)
                    return ResultadoOperacion<TokenRespuestaDto>.SetError("Refresh token revocado.");

                if (tokenBd.ExpiraEn < DateTime.UtcNow)
                    return ResultadoOperacion<TokenRespuestaDto>.SetError("Refresh token expirado.");

                if (tokenBd.Usuario.Estado != TipoEstadoUsuario.Activo)
                    return ResultadoOperacion<TokenRespuestaDto>.SetError("Tu cuenta no está activa.");

                // Rotación: revocamos el viejo y emitimos uno nuevo
                tokenBd.Revocado = true;

                var respuesta = await EmitirTokensAsync(tokenBd.Usuario, direccionIp, agenteUsuario);
                await _context.SaveChangesAsync();

                return ResultadoOperacion<TokenRespuestaDto>.SetExito(respuesta);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<TokenRespuestaDto>.SetError("Error al refrescar token: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // LOGOUT
        // ─────────────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<bool>> LogoutAsync(string refreshToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(refreshToken))
                    return ResultadoOperacion<bool>.SetError("Refresh token requerido.");

                var hash = _jwtService.HashearRefreshToken(refreshToken);
                var tokenBd = await _context.TokensRefresco
                    .FirstOrDefaultAsync(t => t.TokenHash == hash);

                if (tokenBd == null)
                    return ResultadoOperacion<bool>.SetExito(true); // ya no existe, idempotente

                tokenBd.Revocado = true;
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.SetError("Error al cerrar sesión: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // ¿EXISTE ALGÚN ADMINISTRADOR?
        // (Útil para el bootstrap inicial — crear el primer admin sin auth)
        // ─────────────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<bool>> ExisteAdministradorAsync()
        {
            try
            {
                var existe = await _context.UsuarioRoles
                    .AnyAsync(ur => ur.RolId == (short)TipoRol.Admin);
                return ResultadoOperacion<bool>.SetExito(existe);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.SetError("Error: " + ex.Message);
            }
        }

        // ═════════════════════════════════════════════════════════════
        // HELPERS PRIVADOS
        // ═════════════════════════════════════════════════════════════

        private async Task<ResultadoOperacion<bool>> ValidarRegistroComunAsync(string correo, PersonaDto personaDto)
        {
            // Validar campos de Persona con la lógica de dominio del modelo
            var personaTemp = personaDto.ToEntidad();
            var validacionPersona = personaTemp.Validar();
            if (!validacionPersona.Exito)
                return ResultadoOperacion<bool>.SetError(validacionPersona.Mensaje);

            // Correo único
            if (await _context.Usuarios.AnyAsync(u => u.Correo == correo.ToLower()))
                return ResultadoOperacion<bool>.SetError("Ya existe una cuenta con este correo.");

            // Documento único por tipo
            var tipoDocumento = (TipoDocumentoIdentidad)personaDto.TipoDocumento.Id;
            if (await _context.Personas.AnyAsync(p =>
                    p.TipoDocumento == tipoDocumento &&
                    p.NumeroDocumento == personaDto.NumeroDocumento))
                return ResultadoOperacion<bool>.SetError("Ya existe una persona registrada con este documento.");

            return ResultadoOperacion<bool>.SetExito(true);
        }

        private static Usuario CrearUsuario(int personaId, string correo, string contrasena, bool forzarCambioClave = false)
        {
            return new Usuario
            {
                PersonaId = personaId,
                Correo = correo.ToLower(),
                Contrasena = BCrypt.Net.BCrypt.HashPassword(contrasena),
                CorreoConfirmado = false,
                ForzarCambioClave = forzarCambioClave,
                Estado = TipoEstadoUsuario.Activo,
                FechaAlta = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Genera una clave alfanumérica criptográficamente segura.
        /// Garantiza al menos: 1 mayúscula, 1 minúscula y 1 dígito —
        /// para cumplir las políticas habituales de "contraseña fuerte".
        /// </summary>
        private static string GenerarClaveAleatoria(int longitud)
        {
            if (longitud < 6) longitud = 6;

            const string MAY = "ABCDEFGHJKLMNPQRSTUVWXYZ";   // sin I,O
            const string MIN = "abcdefghijkmnpqrstuvwxyz";   // sin l,o
            const string NUM = "23456789";                   // sin 0,1
            const string TODO = MAY + MIN + NUM;

            var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            char Pick(string set)
            {
                var buf = new byte[4];
                rng.GetBytes(buf);
                var idx = (int)(BitConverter.ToUInt32(buf, 0) % (uint)set.Length);
                return set[idx];
            }

            var chars = new char[longitud];
            chars[0] = Pick(MAY);
            chars[1] = Pick(MIN);
            chars[2] = Pick(NUM);
            for (int i = 3; i < longitud; i++) chars[i] = Pick(TODO);

            // Fisher–Yates shuffle para no exponer la posición fija
            for (int i = chars.Length - 1; i > 0; i--)
            {
                var buf = new byte[4];
                rng.GetBytes(buf);
                var j = (int)(BitConverter.ToUInt32(buf, 0) % (uint)(i + 1));
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
            return new string(chars);
        }

        /// <summary>
        /// Envía el correo de creación de usuario y devuelve si tuvo éxito.
        /// El registro NO se revierte si el correo falla — el admin podrá
        /// regenerar la clave manualmente.
        /// </summary>
        private async Task<bool> IntentarEnviarCreacionAsync(
            int usuarioId, string correo, string clave)
        {
            try
            {
                await _notificacionServicio.CrearAsync(
                    usuarioId,
                    TipoNotificacion.CreacionUsuario,
                    "¡Bienvenido a Artesanías Perú!",
                    "Tu cuenta fue creada correctamente.",
                    null,
                    plantillaEmail: PlantillaCorreo.CreacionUsuario,
                    placeholdersEmail: new Dictionary<string, string>
                    {
                        ["usuario"] = correo,
                        ["clave"] = clave
                    });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Genera access token + refresh token, guarda el refresh hasheado en BD
        /// y devuelve el DTO con todo lo que el cliente necesita para renderizar.
        /// </summary>
        private async Task<TokenRespuestaDto> EmitirTokensAsync(
            Usuario usuario, string? direccionIp, string? agenteUsuario,
            bool omitirForzarCambioClave = false)
        {
            var roles = usuario.UsuarioRoles.Select(ur => ur.Rol.Nombre).ToList();
            var vendedorId = usuario.Vendedor?.VendedorId;

            var (token, expira) = _jwtService.GenerarToken(
                usuario.UsuarioId,
                usuario.Correo,
                usuario.PersonaId,
                roles,
                vendedorId,
                DURACION_TOKEN_MINUTOS);

            var refreshPlano = _jwtService.GenerarRefreshToken();
            var refreshHash = _jwtService.HashearRefreshToken(refreshPlano);

            _context.TokensRefresco.Add(new TokenRefresco
            {
                UsuarioId = usuario.UsuarioId,
                TokenHash = refreshHash,
                ExpiraEn = DateTime.UtcNow.AddDays(DURACION_REFRESH_DIAS),
                Revocado = false,
                FechaEmision = DateTime.UtcNow,
                DireccionIp = direccionIp,
                AgenteUsuario = agenteUsuario
            });

            var nombreCompleto = $"{usuario.Persona.Nombres} {usuario.Persona.ApellidoPaterno} {usuario.Persona.ApellidoMaterno}"
                .Replace("  ", " ").Trim();

            return new TokenRespuestaDto
            {
                Token = token,
                RefreshToken = refreshPlano,
                ExpiraEn = expira,
                UsuarioId = usuario.UsuarioId,
                Correo = usuario.Correo,
                NombreCompleto = nombreCompleto,
                Roles = roles,
                VendedorId = vendedorId,
                ForzarCambioClave = omitirForzarCambioClave ? false : usuario.ForzarCambioClave,
                Persona = usuario.Persona.ToDto()
            };
        }

        private async Task<ResultadoOperacion<LoginRespuestaDto>> ProcesarAccesoUsuarioAsync(
            Usuario usuario,
            string? direccionIp,
            string? agenteUsuario,
            bool esLoginExterno = false)
        {
            if (usuario.Estado != TipoEstadoUsuario.Activo)
                return ResultadoOperacion<LoginRespuestaDto>.SetError(
                    "Tu cuenta no está activa. Comunícate con el administrador.");

            var roles = usuario.UsuarioRoles.Select(ur => ur.Rol.Nombre).ToList();
            var requiere2Fa = roles.Any(r => ROLES_CON_2FA.Contains(r));

            if (!requiere2Fa)
            {
                usuario.UltimoAcceso = DateTime.UtcNow;
                var token = await EmitirTokensAsync(
                    usuario, direccionIp, agenteUsuario, omitirForzarCambioClave: esLoginExterno);
                await _context.SaveChangesAsync();
                return ResultadoOperacion<LoginRespuestaDto>.SetExito(new LoginRespuestaDto
                {
                    Requiere2Fa = false,
                    Token = token
                });
            }

            var clave = _configuration["TwoFactor:ClaveCifrado"]!;
            var emisor = _configuration["TwoFactor:Emisor"] ?? "TiendaVirtual";

            if (!usuario.TwoFactorEnabled || string.IsNullOrEmpty(usuario.TwoFactorSecret))
            {
                var secretoNuevo = _twoFactorService.GenerarSecreto();
                var secretoCifrado = _twoFactorService.CifrarSecreto(secretoNuevo, clave);
                var tokenTemporalSetup = _jwtService.GenerarTokenTemporal2Fa(
                    usuario.UsuarioId, secretoCifrado);

                return ResultadoOperacion<LoginRespuestaDto>.SetExito(new LoginRespuestaDto
                {
                    Requiere2Fa = true,
                    DebeConfigurar = true,
                    TokenTemporal = tokenTemporalSetup,
                    QrBase64 = _twoFactorService.GenerarQrComoBase64(secretoNuevo, usuario.Correo, emisor),
                    SecretoManual = secretoNuevo,
                    Mensaje = "Escanea el código QR con tu app autenticadora (Google Authenticator, Authy, etc.) e ingresa el código de 6 dígitos."
                });
            }

            var tokenTemporal = _jwtService.GenerarTokenTemporal2Fa(usuario.UsuarioId);
            return ResultadoOperacion<LoginRespuestaDto>.SetExito(new LoginRespuestaDto
            {
                Requiere2Fa = true,
                DebeConfigurar = false,
                TokenTemporal = tokenTemporal,
                Mensaje = "Ingresa el código de 6 dígitos de tu app autenticadora."
            });
        }

        private async Task<ResultadoOperacion<GoogleTokenPayload>> ValidarTokenGoogleAsync(string idToken)
        {
            if (string.IsNullOrWhiteSpace(idToken))
                return ResultadoOperacion<GoogleTokenPayload>.SetError("Token de Google inválido.");

            return await _googleAuth.ValidarIdTokenAsync(idToken);
        }

        private IQueryable<Usuario> QueryUsuarioConRelaciones() =>
            _context.Usuarios
                .Include(u => u.Persona)
                .Include(u => u.UsuarioRoles).ThenInclude(ur => ur.Rol)
                .Include(u => u.Vendedor);
    }
}
