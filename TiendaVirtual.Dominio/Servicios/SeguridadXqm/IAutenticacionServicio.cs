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
        /// "Olvidé mi contraseña". Genera una clave aleatoria, la guarda
        /// hasheada y la envía por correo. Devuelve un mensaje genérico
        /// (no revela si el correo existe o no, para evitar enumeración).
        /// </summary>
        Task<ResultadoOperacion<string>> RecuperarClaveAsync(string correo);

        Task<ResultadoOperacion<bool>> CambiarPasswordAsync(
            int usuarioId, string contrasenaActual, string contrasenaNueva);

        /// <summary>
        /// Primer cambio tras registro o recuperación, sin sesión (correo + clave temporal).
        /// </summary>
        Task<ResultadoOperacion<bool>> EstablecerContrasenaInicialAsync(
            string correo, string contrasenaActual, string contrasenaNueva);
    }
}
