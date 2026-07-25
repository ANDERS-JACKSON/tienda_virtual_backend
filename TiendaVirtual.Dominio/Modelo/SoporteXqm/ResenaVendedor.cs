using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Dominio.Modelo.VentaXqm;

namespace TiendaVirtual.Dominio.Modelo.SoporteXqm
{
    public class ResenaVendedor
    {
        public long ResenaId { get; set; }

        [Required]
        public int VendedorId { get; set; }

        [Required]
        public Guid SubordenId { get; set; }

        [Required]
        public Guid ClienteId { get; set; }

        public short Calificacion { get; set; }
        public string? Comentario { get; set; }
        public DateTime Fecha { get; set; }

        public virtual Vendedor Vendedor { get; set; } = null!;
        public virtual Suborden Suborden { get; set; } = null!;
        public virtual Usuario Cliente { get; set; } = null!;
    }
}
