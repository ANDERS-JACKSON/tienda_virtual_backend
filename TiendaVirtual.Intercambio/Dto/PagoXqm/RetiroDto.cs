using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.PagoXqm
{
    public class RetiroDto
    {
        public Guid RetiroId { get; set; }
        public int VendedorId { get; set; }
        public Guid CuentaId { get; set; }
        public decimal Monto { get; set; }
        public EnumeracionDto Estado { get; set; } = null!;
        public Guid? ProcesadoPor { get; set; }
        public string? ReferenciaTransferencia { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public DateTime? FechaCompletado { get; set; }
    }
}
