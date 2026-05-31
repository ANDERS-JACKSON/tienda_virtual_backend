using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Dominio.Modelo.ConfiguracionXqm
{
    public class Correo
    {
        public int CorreoId { get; set; }
        public string? Remitente { get; set; }
        public string? CorreoElectronico { get; set; }
        public short? Puerto { get; set; }
        public string? Contrasenia { get; set; }
        public string? ServidorSmtp { get; set; }

        public string? AsuntoCreacionUsuario { get; set; }
        public string? CuerpoCreacionUsuario { get; set; }
        public string? AsuntoRecuperacionClave { get; set; }
        public string? CuerpoRecuperacionClave { get; set; }
        public string? AsuntoNuevoPedidoVendedor { get; set; }
        public string? CuerpoNuevoPedidoVendedor { get; set; }
        public string? AsuntoPedidoEnviadoCliente { get; set; }
        public string? CuerpoPedidoEnviadoCliente { get; set; }
        public string? AsuntoVerificacionResultado { get; set; }
        public string? CuerpoVerificacionResultado { get; set; }
    }
}
