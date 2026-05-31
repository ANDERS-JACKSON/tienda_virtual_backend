using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;
using TiendaVirtual.Dominio.Modelo.VentaXqm;
using TiendaVirtual.Dominio.Utilidad;

namespace TiendaVirtual.Dominio.Modelo.SoporteXqm
{
    public class Reclamo
    {
        public long ReclamoId { get; set; }

        [Required]
        public long SubordenId { get; set; }

        [Required]
        public int AbiertoPor { get; set; }

        [EnumValorValido]
        public TipoMotivoReclamo Motivo { get; set; }

        public string? Descripcion { get; set; }
        public string? Evidencias { get; set; }   // JSONB

        [EnumValorValido]
        public TipoEstadoReclamo Estado { get; set; }

        public string? NotasResolucion { get; set; }
        public int? ResueltoPor { get; set; }
        public decimal? MontoReembolso { get; set; }
        public DateTime FechaApertura { get; set; }
        public DateTime? FechaResolucion { get; set; }

        public virtual Suborden Suborden { get; set; } = null!;
        public virtual Usuario AbiertoPorUsuario { get; set; } = null!;
        public virtual Usuario? ResueltoPorUsuario { get; set; }
        public virtual ICollection<MensajeReclamo> Mensajes { get; set; } = new List<MensajeReclamo>();
    }
}
