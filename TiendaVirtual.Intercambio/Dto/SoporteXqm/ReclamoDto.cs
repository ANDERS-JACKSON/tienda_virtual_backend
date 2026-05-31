using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.SoporteXqm
{
    public class ReclamoDto
    {
        public long ReclamoId { get; set; }
        public long SubordenId { get; set; }
        public int AbiertoPor { get; set; }
        public EnumeracionDto Motivo { get; set; } = null!;
        public string? Descripcion { get; set; }
        public string? Evidencias { get; set; }
        public EnumeracionDto Estado { get; set; } = null!;
        public string? NotasResolucion { get; set; }
        public int? ResueltoPor { get; set; }
        public decimal? MontoReembolso { get; set; }
        public DateTime FechaApertura { get; set; }
        public DateTime? FechaResolucion { get; set; }
    }
}
