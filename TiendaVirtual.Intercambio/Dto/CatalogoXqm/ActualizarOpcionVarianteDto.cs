using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.CatalogoXqm
{
    public class ActualizarOpcionVarianteDto
    {
        [Required]
        [MaxLength(80)]
        public string Nombre { get; set; } = null!;

        public int Orden { get; set; }
        public bool Activo { get; set; }
    }
}
