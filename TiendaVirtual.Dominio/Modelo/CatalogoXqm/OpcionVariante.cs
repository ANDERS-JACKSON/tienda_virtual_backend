using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Dominio.Modelo.CatalogoXqm
{
    public class OpcionVariante
    {
        public int OpcionVarianteId { get; set; }

        [Required]
        public string Nombre { get; set; } = null!;

        public int Orden { get; set; }
        public bool Activo { get; set; }

        public virtual ICollection<VarianteProducto> Variantes { get; set; } = new List<VarianteProducto>();
    }
}
