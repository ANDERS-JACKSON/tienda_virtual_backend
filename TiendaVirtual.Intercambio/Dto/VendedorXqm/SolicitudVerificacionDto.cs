using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    public class SolicitudVerificacionDto
    {
        public int SolicitudId { get; set; }
        public int VendedorId { get; set; }
        public string NombreTienda { get; set; } = null!; 
        public string CorreoVendedor { get; set; } = null!;
        public EnumeracionDto Estado { get; set; } = null!;
        public string DocumentoFrenteUrl { get; set; } = null!;
        public string? DocumentoReversoUrl { get; set; }
        public string? SelfieDocumentoUrl { get; set; }
        public string? FotosProductos { get; set; }
        public int? VerificadorId { get; set; }
        public string? NotasRevisor { get; set; }
        public string? MotivoRechazo { get; set; }
        public DateTime FechaEnvio { get; set; }
        public DateTime? FechaRevision { get; set; }
    }
}
