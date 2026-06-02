using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.SeguridadXqm
{
    public class CrearDireccionDto
    {
        [MaxLength(50)]
        public string? Etiqueta { get; set; }

        [Required, MaxLength(200)]
        public string NombreReceptor { get; set; } = null!;

        [MaxLength(20)]
        public string? Telefono { get; set; }

        [Required, MaxLength(100)]
        public string Departamento { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Provincia { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Distrito { get; set; } = null!;

        [Required, MaxLength(300)]
        public string DireccionLinea { get; set; } = null!;

        [MaxLength(300)]
        public string? Referencia { get; set; }

        public bool EsPredeterminada { get; set; }
    }

    public class ActualizarDireccionDto : CrearDireccionDto { }
}
