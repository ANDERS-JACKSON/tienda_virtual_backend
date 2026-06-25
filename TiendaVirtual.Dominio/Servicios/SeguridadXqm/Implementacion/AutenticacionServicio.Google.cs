using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Extensiones.SeguridadXqm;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;
using TiendaVirtual.Dominio.Servicios.SeguridadXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;

namespace TiendaVirtual.Dominio.Servicios.SeguridadXqm.Implementacion
{
    public partial class AutenticacionServicio
    {
        private const string PROVEEDOR_GOOGLE = "GOOGLE";

        public async Task<ResultadoOperacion<LoginRespuestaDto>> LoginConGoogleAsync(
            GoogleLoginDto dto, string? direccionIp, string? agenteUsuario)
        {
            try
            {
                if (dto == null)
                    return ResultadoOperacion<LoginRespuestaDto>.SetError("Datos inválidos.");

                var validacion = await ValidarTokenGoogleAsync(dto.IdToken);
                if (!validacion.Exito || validacion.Datos == null)
                    return ResultadoOperacion<LoginRespuestaDto>.SetError(validacion.Mensaje);

                var payload = validacion.Datos;
                if (!payload.EmailVerified)
                    return ResultadoOperacion<LoginRespuestaDto>.SetError(
                        "Tu correo de Google no está verificado. Verifícalo en tu cuenta de Google e intenta de nuevo.");

                var usuarioPorGoogle = await ObtenerUsuarioPorVinculoGoogleAsync(payload.Subject);
                if (usuarioPorGoogle != null)
                    return await ProcesarAccesoUsuarioAsync(
                        usuarioPorGoogle, direccionIp, agenteUsuario, esLoginExterno: true);

                var usuarioPorCorreo = await QueryUsuarioConRelaciones()
                    .FirstOrDefaultAsync(u => u.Correo == payload.Email);

                if (usuarioPorCorreo != null)
                {
                    var errorVinculo = await VincularGoogleInternoAsync(usuarioPorCorreo.UsuarioId, payload.Subject);
                    if (!errorVinculo.Exito)
                        return ResultadoOperacion<LoginRespuestaDto>.SetError(errorVinculo.Mensaje);

                    return await ProcesarAccesoUsuarioAsync(
                        usuarioPorCorreo, direccionIp, agenteUsuario, esLoginExterno: true);
                }

                return ResultadoOperacion<LoginRespuestaDto>.SetExito(new LoginRespuestaDto
                {
                    Requiere2Fa = false,
                    RequiereCompletarRegistro = true,
                    RegistroPendiente = MapearRegistroPendiente(payload)
                });
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<LoginRespuestaDto>.SetError(
                    "Error al iniciar sesión con Google: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<LoginRespuestaDto>> CompletarRegistroGoogleAsync(
            CompletarRegistroGoogleDto dto, string? direccionIp, string? agenteUsuario)
        {
            using var transaccion = await _context.Database.BeginTransactionAsync();
            try
            {
                if (dto == null)
                    return ResultadoOperacion<LoginRespuestaDto>.SetError("Datos inválidos.");

                var validacion = await ValidarTokenGoogleAsync(dto.IdToken);
                if (!validacion.Exito || validacion.Datos == null)
                    return ResultadoOperacion<LoginRespuestaDto>.SetError(validacion.Mensaje);

                var payload = validacion.Datos;
                if (!payload.EmailVerified)
                    return ResultadoOperacion<LoginRespuestaDto>.SetError(
                        "Tu correo de Google no está verificado.");

                if (await _context.UsuariosLoginExterno.AnyAsync(le =>
                        le.Proveedor == PROVEEDOR_GOOGLE && le.SubjectId == payload.Subject))
                    return ResultadoOperacion<LoginRespuestaDto>.SetError(
                        "Esta cuenta de Google ya está registrada. Inicia sesión.");

                var validacionComun = await ValidarRegistroComunAsync(payload.Email, dto.Persona);
                if (!validacionComun.Exito)
                    return ResultadoOperacion<LoginRespuestaDto>.SetError(validacionComun.Mensaje);

                var persona = dto.Persona.ToEntidad();
                persona.CorreoElectronico = payload.Email;
                _context.Personas.Add(persona);
                await _context.SaveChangesAsync();

                var usuario = CrearUsuarioOAuth(persona.PersonaId, payload.Email);
                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                _context.UsuarioRoles.Add(new UsuarioRol
                {
                    UsuarioId = usuario.UsuarioId,
                    RolId = (short)TipoRol.Cliente
                });

                _context.UsuariosLoginExterno.Add(new UsuarioLoginExterno
                {
                    UsuarioId = usuario.UsuarioId,
                    Proveedor = PROVEEDOR_GOOGLE,
                    SubjectId = payload.Subject,
                    FechaVinculacion = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                var usuarioCompleto = await QueryUsuarioConRelaciones()
                    .FirstAsync(u => u.UsuarioId == usuario.UsuarioId);

                return await ProcesarAccesoUsuarioAsync(
                    usuarioCompleto, direccionIp, agenteUsuario, esLoginExterno: true);
            }
            catch (Exception ex)
            {
                await transaccion.RollbackAsync();
                return ResultadoOperacion<LoginRespuestaDto>.SetError(
                    "Error al completar el registro con Google: " + ex.Message);
            }
        }

        private async Task<Usuario?> ObtenerUsuarioPorVinculoGoogleAsync(string subjectId)
        {
            var usuarioId = await _context.UsuariosLoginExterno
                .AsNoTracking()
                .Where(le => le.Proveedor == PROVEEDOR_GOOGLE && le.SubjectId == subjectId)
                .Select(le => le.UsuarioId)
                .FirstOrDefaultAsync();

            if (usuarioId == 0)
                return null;

            return await QueryUsuarioConRelaciones()
                .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);
        }

        private async Task<ResultadoOperacion<bool>> VincularGoogleInternoAsync(int usuarioId, string subjectId)
        {
            var vinculadoAOtro = await _context.UsuariosLoginExterno.AnyAsync(le =>
                le.Proveedor == PROVEEDOR_GOOGLE &&
                le.SubjectId == subjectId &&
                le.UsuarioId != usuarioId);

            if (vinculadoAOtro)
                return ResultadoOperacion<bool>.SetError(
                    "Esta cuenta de Google ya está vinculada a otro usuario.");

            var yaTieneGoogle = await _context.UsuariosLoginExterno.AnyAsync(le =>
                le.UsuarioId == usuarioId && le.Proveedor == PROVEEDOR_GOOGLE);

            if (!yaTieneGoogle)
            {
                _context.UsuariosLoginExterno.Add(new UsuarioLoginExterno
                {
                    UsuarioId = usuarioId,
                    Proveedor = PROVEEDOR_GOOGLE,
                    SubjectId = subjectId,
                    FechaVinculacion = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            return ResultadoOperacion<bool>.SetExito(true);
        }

        private static RegistroGooglePendienteDto MapearRegistroPendiente(GoogleTokenPayload payload)
        {
            var (nombres, apellidoPaterno, apellidoMaterno) = SepararNombreGoogle(payload);

            return new RegistroGooglePendienteDto
            {
                Correo = payload.Email,
                Nombres = nombres,
                ApellidoPaterno = apellidoPaterno,
                ApellidoMaterno = apellidoMaterno
            };
        }

        private static (string? Nombres, string? ApellidoPaterno, string? ApellidoMaterno) SepararNombreGoogle(
            GoogleTokenPayload payload)
        {
            if (!string.IsNullOrWhiteSpace(payload.GivenName))
            {
                return (
                    payload.GivenName.Trim(),
                    payload.FamilyName?.Trim(),
                    null);
            }

            if (string.IsNullOrWhiteSpace(payload.Name))
                return (null, null, null);

            var partes = payload.Name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 1)
                return (partes[0], null, null);

            if (partes.Length == 2)
                return (partes[0], partes[1], null);

            return (partes[0], partes[1], string.Join(' ', partes.Skip(2)));
        }

        private static Usuario CrearUsuarioOAuth(int personaId, string correo)
        {
            var claveInterna = GenerarClaveAleatoria(32);
            return new Usuario
            {
                PersonaId = personaId,
                Correo = correo.ToLower(),
                Contrasena = BCrypt.Net.BCrypt.HashPassword(claveInterna),
                CorreoConfirmado = true,
                ForzarCambioClave = false,
                Estado = TipoEstadoUsuario.Activo,
                FechaAlta = DateTime.UtcNow
            };
        }

        public async Task<ResultadoOperacion<CuentaSeguridadDto>> ObtenerSeguridadCuentaAsync(int usuarioId)
        {
            try
            {
                var tieneGoogle = await _context.UsuariosLoginExterno.AsNoTracking()
                    .AnyAsync(le => le.UsuarioId == usuarioId && le.Proveedor == PROVEEDOR_GOOGLE);

                return ResultadoOperacion<CuentaSeguridadDto>.SetExito(new CuentaSeguridadDto
                {
                    TieneVinculoGoogle = tieneGoogle,
                    PuedeVincularGoogle = !tieneGoogle
                });
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<CuentaSeguridadDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> VincularGoogleAsync(int usuarioId, GoogleLoginDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.IdToken))
                    return ResultadoOperacion<bool>.SetError("Token de Google inválido.");

                var validacion = await ValidarTokenGoogleAsync(dto.IdToken);
                if (!validacion.Exito || validacion.Datos == null)
                    return ResultadoOperacion<bool>.SetError(validacion.Mensaje);

                var payload = validacion.Datos;
                if (!payload.EmailVerified)
                    return ResultadoOperacion<bool>.SetError(
                        "Tu correo de Google no está verificado.");

                var usuario = await _context.Usuarios.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);

                if (usuario == null)
                    return ResultadoOperacion<bool>.SetError("Usuario no encontrado.");

                if (usuario.Correo != payload.Email)
                    return ResultadoOperacion<bool>.SetError(
                        "El correo de Google debe coincidir con el de tu cuenta (" + usuario.Correo + ").");

                var yaVinculado = await _context.UsuariosLoginExterno.AnyAsync(le =>
                    le.UsuarioId == usuarioId && le.Proveedor == PROVEEDOR_GOOGLE);

                if (yaVinculado)
                    return ResultadoOperacion<bool>.SetError("Tu cuenta ya tiene Google vinculado.");

                return await VincularGoogleInternoAsync(usuarioId, payload.Subject);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.SetError("Error al vincular Google: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> DesvincularGoogleAsync(int usuarioId, string contrasena)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(contrasena))
                    return ResultadoOperacion<bool>.SetError("La contraseña es obligatoria.");

                var usuario = await _context.Usuarios.FindAsync(usuarioId);
                if (usuario == null)
                    return ResultadoOperacion<bool>.SetError("Usuario no encontrado.");

                if (!BCrypt.Net.BCrypt.Verify(contrasena, usuario.Contrasena))
                    return ResultadoOperacion<bool>.SetError("Contraseña incorrecta.");

                var vinculo = await _context.UsuariosLoginExterno.FirstOrDefaultAsync(le =>
                    le.UsuarioId == usuarioId && le.Proveedor == PROVEEDOR_GOOGLE);

                if (vinculo == null)
                    return ResultadoOperacion<bool>.SetError("Tu cuenta no tiene Google vinculado.");

                _context.UsuariosLoginExterno.Remove(vinculo);
                await _context.SaveChangesAsync();

                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.SetError("Error al desvincular Google: " + ex.Message);
            }
        }
    }
}
