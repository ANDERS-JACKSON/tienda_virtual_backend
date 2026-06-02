using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.CatalogoXqm
{
    public class ActualizarImagenDto
    {
        [MaxLength(250)]
        public string? TextoAlt { get; set; }

        public int Orden { get; set; }
        public bool EsPrincipal { get; set; }
    }
}
