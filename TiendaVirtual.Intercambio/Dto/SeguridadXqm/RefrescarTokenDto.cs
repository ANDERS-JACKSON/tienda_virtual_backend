using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.SeguridadXqm
{
    public class RefrescarTokenDto
    {
        [Required]
        public string RefreshToken { get; set; } = null!;
    }
}
