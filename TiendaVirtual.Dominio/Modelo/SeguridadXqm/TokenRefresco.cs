using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Dominio.Modelo.SeguridadXqm
{
    public class TokenRefresco
    {
        public long TokenId { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        [Required]
        public string TokenHash { get; set; } = null!;

        public DateTime ExpiraEn { get; set; }
        public bool Revocado { get; set; }
        public DateTime FechaEmision { get; set; }
        public string? DireccionIp { get; set; }
        public string? AgenteUsuario { get; set; }

        public virtual Usuario Usuario { get; set; } = null!;
    }
}
