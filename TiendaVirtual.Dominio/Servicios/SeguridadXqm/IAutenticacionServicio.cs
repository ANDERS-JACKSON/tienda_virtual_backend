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

        Task<ResultadoOperacion<TokenRespuestaDto>> RegistrarClienteAsync(
            RegistroClienteDto dto, string? direccionIp, string? agenteUsuario);

        Task<ResultadoOperacion<TokenRespuestaDto>> RegistrarVendedorAsync(
            RegistroVendedorDto dto, string? direccionIp, string? agenteUsuario);

        Task<ResultadoOperacion<TokenRespuestaDto>> RegistrarAdministradorAsync(
            RegistroAdministradorDto dto, string? direccionIp, string? agenteUsuario);

        Task<ResultadoOperacion<TokenRespuestaDto>> RefrescarTokenAsync(
            string refreshToken, string? direccionIp, string? agenteUsuario);

        Task<ResultadoOperacion<bool>> LogoutAsync(string refreshToken);
    }
}
