using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.VentaXqm
{
    public class EnvioDto
    {
        public Guid EnvioId { get; set; }
        public Guid SubordenId { get; set; }
        public string Transportista { get; set; } = null!;
        public string? CodigoOrdenAgencia { get; set; }
        public string? NumeroSeguimiento { get; set; }
        public string? ClaveRecojo { get; set; }
        public string? Detalles { get; set; }
        public string? ComprobanteUrl { get; set; }
        public decimal? MontoComprobante { get; set; }
        public DateOnly? FechaEntregaReal { get; set; }
    }
}
