using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;

namespace TiendaVirtual.Dominio.Servicios.SeguridadXqm
{
    public interface IAutenticacionServicio
    {
        Task<ResultadoOperacion<LoginRespuestaDto>> LoginAsync(
            LoginDto dto, string? direccionIp, string? agenteUsuario);

        Task<ResultadoOperacion<TokenRespuestaDto>> VerificarDosFactoresAsync(
            Verificar2FaRequestDto dto, string? direccionIp, string? agenteUsuario);

        Task<ResultadoOperacion<RegistroRespuestaDto>> RegistrarClienteAsync(
            RegistroClienteDto dto, string? direccionIp, string? agenteUsuario);

        Task<ResultadoOperacion<RegistroRespuestaDto>> RegistrarVendedorAsync(
            RegistroVendedorDto dto, string? direccionIp, string? agenteUsuario);

        Task<ResultadoOperacion<RegistroRespuestaDto>> RegistrarAdministradorAsync(
            RegistroAdministradorDto dto, string? direccionIp, string? agenteUsuario);

        Task<ResultadoOperacion<TokenRespuestaDto>> RefrescarTokenAsync(
            string refreshToken, string? direccionIp, string? agenteUsuario);

        Task<ResultadoOperacion<bool>> LogoutAsync(string refreshToken);

        /// <summary>
        /// Envía un código de un solo uso para elegir una contraseña nueva.
        /// La respuesta no dice si el correo tiene cuenta.
        /// </summary>
        Task<ResultadoOperacion<string>> RecuperarClaveAsync(string correo, string? direccionIp);

        /// <summary>
        /// Canjea el código vigente por un ticket corto para definir la contraseña.
        /// </summary>
        Task<ResultadoOperacion<string>> VerificarRecuperacionClaveAsync(string correo, string codigo);

        /// <summary>
        /// Guarda la contraseña nueva si el ticket sigue vigente. El ticket se usa una sola vez.
        /// </summary>
        Task<ResultadoOperacion<bool>> ConfirmarRecuperacionClaveAsync(
            string correo, string ticket, string contrasenaNueva);

        Task<ResultadoOperacion<bool>> CambiarPasswordAsync(
            Guid usuarioId, string contrasenaActual, string contrasenaNueva);

        /// <summary>
        /// Primer cambio tras registro o recuperación, sin sesión (correo + clave temporal).
        /// </summary>
        Task<ResultadoOperacion<bool>> EstablecerContrasenaInicialAsync(
            string correo, string contrasenaActual, string contrasenaNueva);

        Task<ResultadoOperacion<LoginRespuestaDto>> LoginConGoogleAsync(
            GoogleLoginDto dto, string? direccionIp, string? agenteUsuario);

        Task<ResultadoOperacion<LoginRespuestaDto>> CompletarRegistroGoogleAsync(
            CompletarRegistroGoogleDto dto, string? direccionIp, string? agenteUsuario);

        Task<ResultadoOperacion<CuentaSeguridadDto>> ObtenerSeguridadCuentaAsync(Guid usuarioId);

        Task<ResultadoOperacion<bool>> VincularGoogleAsync(Guid usuarioId, GoogleLoginDto dto);

        Task<ResultadoOperacion<bool>> DesvincularGoogleAsync(Guid usuarioId, string contrasena);

        Task<ResultadoOperacion<string>> SolicitarCodigoAccesoAsync(string correo, string? direccionIp);

        Task<ResultadoOperacion<LoginRespuestaDto>> VerificarCodigoAccesoAsync(
            string correo, string codigo, string? direccionIp, string? agenteUsuario);

        Task<ResultadoOperacion<LoginRespuestaDto>> CompletarRegistroCodigoAsync(
            CompletarRegistroCodigoDto dto, string? direccionIp, string? agenteUsuario);

        Task<ResultadoOperacion<LoginRespuestaDto>> PrepararCompraInvitadoAsync(
            PrepararCompraInvitadoDto dto, string? direccionIp, string? agenteUsuario);
    }
}
