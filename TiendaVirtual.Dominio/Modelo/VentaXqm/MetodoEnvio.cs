using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Dominio.Modelo.VentaXqm
{
    public class MetodoEnvio
    {
        public int MetodoEnvioId { get; set; }

        [Required]
        public string Codigo { get; set; } = null!;

        [Required]
        public string Nombre { get; set; } = null!;

        public string? Descripcion { get; set; }

        public decimal MontoBase { get; set; }
        public int TiempoEstimadoDias { get; set; }
        public bool Activo { get; set; }
        public int Orden { get; set; }

        public virtual ICollection<Suborden> Subordenes { get; set; } = new List<Suborden>();
    }
}
