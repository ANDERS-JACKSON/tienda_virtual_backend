using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.VentaXqm
{
    public class EnvioDto
    {
        public long EnvioId { get; set; }
        public long SubordenId { get; set; }
        public string Transportista { get; set; } = null!;
        public string? NumeroSeguimiento { get; set; }
        public string? ComprobanteUrl { get; set; }
        public decimal? MontoComprobante { get; set; }
        public DateOnly? FechaEntregaReal { get; set; }
    }
}
