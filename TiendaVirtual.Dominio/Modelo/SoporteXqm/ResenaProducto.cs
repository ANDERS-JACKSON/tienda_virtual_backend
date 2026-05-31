using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;
using TiendaVirtual.Dominio.Modelo.VentaXqm;

namespace TiendaVirtual.Dominio.Modelo.SoporteXqm
{
    public class ResenaProducto
    {
        public long ResenaId { get; set; }

        [Required]
        public int ProductoId { get; set; }

        [Required]
        public long ItemOrdenId { get; set; }

        [Required]
        public int ClienteId { get; set; }

        public short Calificacion { get; set; }
        public string? Titulo { get; set; }
        public string? Comentario { get; set; }
        public string? Imagenes { get; set; }   // JSONB
        public string? RespuestaVendedor { get; set; }
        public DateTime Fecha { get; set; }

        public virtual Producto Producto { get; set; } = null!;
        public virtual ItemOrden ItemOrden { get; set; } = null!;
        public virtual Usuario Cliente { get; set; } = null!;
    }
}
