using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.SoporteXqm;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Dominio.Utilidad;

namespace TiendaVirtual.Dominio.Modelo.VentaXqm
{
    public class Suborden
    {
        public long SubordenId { get; set; }

        [Required]
        public long OrdenId { get; set; }

        [Required]
        public int VendedorId { get; set; }

        [Required]
        public string NumeroSuborden { get; set; } = null!;

        public decimal Subtotal { get; set; }
        public decimal MontoEnvio { get; set; }
        public decimal MontoComision { get; set; }
        public decimal MontoVendedor { get; set; }

        [EnumValorValido]
        public TipoEstadoSuborden Estado { get; set; }

        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaEntrega { get; set; }

        public virtual Orden Orden { get; set; } = null!;
        public virtual Vendedor Vendedor { get; set; } = null!;
        public virtual ICollection<ItemOrden> Items { get; set; } = new List<ItemOrden>();
        public virtual ICollection<Envio> Envios { get; set; } = new List<Envio>();
        public virtual ICollection<Reclamo> Reclamos { get; set; } = new List<Reclamo>();
        public virtual ResenaVendedor? ResenaVendedor { get; set; }
    }
}
