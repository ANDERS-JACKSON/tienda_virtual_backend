using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.SeguridadXqm
{
    public class TokenRespuestaDto
    {
        public string Token { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public DateTime ExpiraEn { get; set; }
        public int UsuarioId { get; set; }
        public string Correo { get; set; } = null!;
        public string NombreCompleto { get; set; } = null!;
        public List<string> Roles { get; set; } = new();
        public int? VendedorId { get; set; }

        /// <summary>Datos de identidad del usuario (para perfil / datos personales).</summary>
        public PersonaDto? Persona { get; set; }

        /// <summary>
        /// Si es true, el cliente debe mostrar el flujo de cambio de contraseña
        /// antes de usar la aplicación con normalidad.
        /// </summary>
        public bool ForzarCambioClave { get; set; }
    }
}
