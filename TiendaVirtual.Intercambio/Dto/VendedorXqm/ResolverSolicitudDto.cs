using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    public class ResolverSolicitudDto
    {
        // Opcional al aprobar, obligatorio al rechazar (validado en el servicio)
        [MaxLength(1000)]
        public string? NotasRevisor { get; set; }

        [MaxLength(500)]
        public string? MotivoRechazo { get; set; }
    }
}
