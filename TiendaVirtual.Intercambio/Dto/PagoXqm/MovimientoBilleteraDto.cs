using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.PagoXqm
{
    public class MovimientoBilleteraDto
    {
        public long MovimientoId { get; set; }
        public int VendedorId { get; set; }
        public EnumeracionDto Tipo { get; set; } = null!;
        public decimal Monto { get; set; }
        public decimal SaldoResultante { get; set; }
        public long? ReferenciaId { get; set; }
        public string? Descripcion { get; set; }
        public DateTime Fecha { get; set; }
    }
}
