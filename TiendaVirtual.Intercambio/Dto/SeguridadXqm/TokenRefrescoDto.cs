using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.SeguridadXqm
{
    public class TokenRefrescoDto
    {
        public long TokenId { get; set; }
        public int UsuarioId { get; set; }
        public string TokenHash { get; set; } = null!;
        public DateTime ExpiraEn { get; set; }
        public bool Revocado { get; set; }
        public DateTime FechaEmision { get; set; }
        public string? DireccionIp { get; set; }
        public string? AgenteUsuario { get; set; }
    }
}
