using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Dominio.Modelo.ConfiguracionXqm
{
    public class AvisoBanner
    {
        public int AvisoBannerId { get; set; }

        [Required]
        public string Texto { get; set; } = null!;

        public bool Activo { get; set; }
        public int Orden { get; set; }
    }
}
