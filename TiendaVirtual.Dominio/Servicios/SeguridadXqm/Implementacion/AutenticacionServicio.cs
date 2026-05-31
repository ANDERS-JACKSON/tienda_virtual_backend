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
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;

namespace TiendaVirtual.Dominio.Servicios.SeguridadXqm.Implementacion
{
    public class AutenticacionServicio : IAutenticacionServicio
    {
        protected readonly TiendaVirtualDbContext _context;
        protected readonly JwtTokenService _jwtService;
        protected readonly ITwoFactorService _twoFactorService;
        protected readonly IConfiguration _configuration;

        private const int DURACION_TOKEN_MINUTOS = 60;
        private const int DURACION_REFRESH_DIAS = 30;

        // Roles que exigen 2FA obligatorio
        private static readonly string[] ROLES_CON_2FA = { "ADMIN", "VERIFICADOR" };

        public AutenticacionServicio(
            TiendaVirtualDbContext context,
            JwtTokenService jwtService,
            ITwoFactorService twoFactorService,
            IConfiguration configuration)
        {
            _context = context;
            _jwtService = jwtService;
            _twoFactorService = twoFactorService;
            _configuration = configuration;
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

                if (usuario.Estado != TipoEstadoUsuario.Activo)
                    return ResultadoOperacion<LoginRespuestaDto>.SetError(
                        "Tu cuenta no está activa. Comunícate con el administrador.");

                var roles = usuario.UsuarioRoles.Select(ur => ur.Rol.Nombre).ToList();
                var requiere2Fa = roles.Any(r => ROLES_CON_2FA.Contains(r));

                // ── Caso: roles sin 2FA (cliente, vendedor) → login completo
                if (!requiere2Fa)
                {
                    usuario.UltimoAcceso = DateTime.UtcNow;
                    var token = await EmitirTokensAsync(usuario, direccionIp, agenteUsuario);
                    await _context.SaveChangesAsync();
                    return ResultadoOperacion<LoginRespuestaDto>.SetExito(new LoginRespuestaDto
                    {
                        Requiere2Fa = false,
                        Token = token
                    });
                }

                // ── Caso: roles con 2FA obligatorio
                var clave = _configuration["TwoFactor:ClaveCifrado"]!;
                var emisor = _configuration["TwoFactor:Emisor"] ?? "TiendaVirtual";

                // ¿Primera vez? El usuario aún no tiene secreto en BD.
                if (!usuario.TwoFactorEnabled || string.IsNullOrEmpty(usuario.TwoFactorSecret))
                {
                    var secretoNuevo = _twoFactorService.GenerarSecreto();
                    var secretoCifrado = _twoFactorService.CifrarSecreto(secretoNuevo, clave);

                    // El secreto cifrado viaja DENTRO del token temporal (no se guarda hasta confirmar)
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

                // 2FA ya configurado → solo pedir código
                var tokenTemporal = _jwtService.GenerarTokenTemporal2Fa(usuario.UsuarioId);
                return ResultadoOperacion<LoginRespuestaDto>.SetExito(new LoginRespuestaDto
                {
                    Requiere2Fa = true,
                    DebeConfigurar = false,
                    TokenTemporal = tokenTemporal,
                    Mensaje = "Ingresa el código de 6 dígitos de tu app autenticadora."
                });
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
        public async Task<ResultadoOperacion<TokenRespuestaDto>> RegistrarClienteAsync(
            RegistroClienteDto dto, string? direccionIp, string? agenteUsuario)
        {
            using var transaccion = await _context.Database.BeginTransactionAsync();
            try
            {
                if (dto == null)
                    return ResultadoOperacion<TokenRespuestaDto>.SetError("El DTO es nulo.");

                var validacionComun = await ValidarRegistroComunAsync(dto.Correo, dto.Persona);
                if (!validacionComun.Exito)
                    return ResultadoOperacion<TokenRespuestaDto>.SetError(validacionComun.Mensaje);

                var persona = dto.Persona.ToEntidad();
                _context.Personas.Add(persona);
                await _context.SaveChangesAsync();

                var usuario = CrearUsuario(persona.PersonaId, dto.Correo, dto.Contrasena);
                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                _context.UsuarioRoles.Add(new UsuarioRol
                {
                    UsuarioId = usuario.UsuarioId,
                    RolId = (short)TipoRol.Cliente
                });
                await _context.SaveChangesAsync();

                // Hidratar navegaciones necesarias para el token
                usuario.Persona = persona;
                usuario.UsuarioRoles = await _context.UsuarioRoles
                    .Include(ur => ur.Rol)
                    .Where(ur => ur.UsuarioId == usuario.UsuarioId)
                    .ToListAsync();

                var respuesta = await EmitirTokensAsync(usuario, direccionIp, agenteUsuario);
                await _context.SaveChangesAsync();

                await transaccion.CommitAsync();
                return ResultadoOperacion<TokenRespuestaDto>.SetExito(respuesta);
            }
            catch (Exception ex)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion<TokenRespuestaDto>.SetError("Error al registrar cliente: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // REGISTRAR VENDEDOR
        // ─────────────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<TokenRespuestaDto>> RegistrarVendedorAsync(
            RegistroVendedorDto dto, string? direccionIp, string? agenteUsuario)
        {
            using var transaccion = await _context.Database.BeginTransactionAsync();
            try
            {
                if (dto == null)
                    return ResultadoOperacion<TokenRespuestaDto>.SetError("El DTO es nulo.");

                var validacionComun = await ValidarRegistroComunAsync(dto.Correo, dto.Persona);
                if (!validacionComun.Exito)
                    return ResultadoOperacion<TokenRespuestaDto>.SetError(validacionComun.Mensaje);

                // Slug único
                var slug = dto.SlugTienda.ToLower();
                if (await _context.Vendedores.AnyAsync(v => v.SlugTienda == slug))
                    return ResultadoOperacion<TokenRespuestaDto>.SetError("Ese nombre de tienda ya está en uso, prueba con otro.");

                var persona = dto.Persona.ToEntidad();
                _context.Personas.Add(persona);
                await _context.SaveChangesAsync();

                var usuario = CrearUsuario(persona.PersonaId, dto.Correo, dto.Contrasena);
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

                usuario.Persona = persona;
                usuario.Vendedor = vendedor;
                usuario.UsuarioRoles = await _context.UsuarioRoles
                    .Include(ur => ur.Rol)
                    .Where(ur => ur.UsuarioId == usuario.UsuarioId)
                    .ToListAsync();

                var respuesta = await EmitirTokensAsync(usuario, direccionIp, agenteUsuario);
                await _context.SaveChangesAsync();

                await transaccion.CommitAsync();
                return ResultadoOperacion<TokenRespuestaDto>.SetExito(respuesta);
            }
            catch (Exception ex)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion<TokenRespuestaDto>.SetError("Error al registrar vendedor: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // REGISTRAR ADMINISTRADOR
        // ─────────────────────────────────────────────────────────────
        public async Task<ResultadoOperacion<TokenRespuestaDto>> RegistrarAdministradorAsync(
            RegistroAdministradorDto dto, string? direccionIp, string? agenteUsuario)
        {
            using var transaccion = await _context.Database.BeginTransactionAsync();
            try
            {
                if (dto == null)
                    return ResultadoOperacion<TokenRespuestaDto>.SetError("El DTO es nulo.");

                var validacionComun = await ValidarRegistroComunAsync(dto.Correo, dto.Persona);
                if (!validacionComun.Exito)
                    return ResultadoOperacion<TokenRespuestaDto>.SetError(validacionComun.Mensaje);

                var persona = dto.Persona.ToEntidad();
                _context.Personas.Add(persona);
                await _context.SaveChangesAsync();

                var usuario = CrearUsuario(persona.PersonaId, dto.Correo, dto.Contrasena);
                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                _context.UsuarioRoles.Add(new UsuarioRol
                {
                    UsuarioId = usuario.UsuarioId,
                    RolId = (short)TipoRol.Admin
                });
                await _context.SaveChangesAsync();

                usuario.Persona = persona;
                usuario.UsuarioRoles = await _context.UsuarioRoles
                    .Include(ur => ur.Rol)
                    .Where(ur => ur.UsuarioId == usuario.UsuarioId)
                    .ToListAsync();

                var respuesta = await EmitirTokensAsync(usuario, direccionIp, agenteUsuario);
                await _context.SaveChangesAsync();

                await transaccion.CommitAsync();
                return ResultadoOperacion<TokenRespuestaDto>.SetExito(respuesta);
            }
            catch (Exception ex)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion<TokenRespuestaDto>.SetError("Error al registrar administrador: " + ex.Message);
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

        private static Usuario CrearUsuario(int personaId, string correo, string contrasena)
        {
            return new Usuario
            {
                PersonaId = personaId,
                Correo = correo.ToLower(),
                Contrasena = BCrypt.Net.BCrypt.HashPassword(contrasena),
                CorreoConfirmado = false,
                ForzarCambioClave = false,
                Estado = TipoEstadoUsuario.Activo,
                FechaAlta = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Genera access token + refresh token, guarda el refresh hasheado en BD
        /// y devuelve el DTO con todo lo que el cliente necesita para renderizar.
        /// </summary>
        private async Task<TokenRespuestaDto> EmitirTokensAsync(
            Usuario usuario, string? direccionIp, string? agenteUsuario)
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
                VendedorId = vendedorId
            };
        }
    }
}
