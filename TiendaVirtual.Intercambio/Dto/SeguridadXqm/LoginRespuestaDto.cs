using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.SeguridadXqm
{
    public class LoginRespuestaDto
    {
        public bool Requiere2Fa { get; set; }

        // Cuando Requiere2Fa = false → login completo
        public TokenRespuestaDto? Token { get; set; }

        // Cuando Requiere2Fa = true → flujo de 2FA
        public bool DebeConfigurar { get; set; }
        public string? TokenTemporal { get; set; }
        public string? QrBase64 { get; set; }
        public string? SecretoManual { get; set; }
        public string? Mensaje { get; set; }

        /// <summary>Usuario nuevo en Google: debe completar DNI y datos de persona.</summary>
        public bool RequiereCompletarRegistro { get; set; }
        public RegistroGooglePendienteDto? RegistroPendiente { get; set; }
    }
}
