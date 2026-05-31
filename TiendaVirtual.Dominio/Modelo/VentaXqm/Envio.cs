using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Dominio.Modelo.VentaXqm
{
    public class Envio
    {
        public long EnvioId { get; set; }

        [Required]
        public long SubordenId { get; set; }

        [Required]
        public string Transportista { get; set; } = null!;

        public string? NumeroSeguimiento { get; set; }
        public string? ComprobanteUrl { get; set; }
        public decimal? MontoComprobante { get; set; }
        public DateOnly? FechaEntregaReal { get; set; }

        public virtual Suborden Suborden { get; set; } = null!;
    }
}
