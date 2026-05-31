using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.PagoXqm;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;
using TiendaVirtual.Dominio.Utilidad;

namespace TiendaVirtual.Dominio.Modelo.VentaXqm
{
    public class Orden
    {
        public long OrdenId { get; set; }

        [Required]
        public string NumeroOrden { get; set; } = null!;

        [Required]
        public int ClienteId { get; set; }

        public decimal Subtotal { get; set; }
        public decimal TotalEnvio { get; set; }
        public decimal TotalDescuento { get; set; }
        public decimal Total { get; set; }

        [Required]
        public string CorreoCliente { get; set; } = null!;

        public string? TelefonoCliente { get; set; }

        [Required]
        public string DireccionEnvio { get; set; } = null!;   // JSONB

        [EnumValorValido]
        public TipoEstadoOrden Estado { get; set; }

        public DateTime Fecha { get; set; }

        public virtual Usuario Cliente { get; set; } = null!;
        public virtual ICollection<Suborden> Subordenes { get; set; } = new List<Suborden>();
        public virtual ICollection<Transaccion> Transacciones { get; set; } = new List<Transaccion>();
    }
}
