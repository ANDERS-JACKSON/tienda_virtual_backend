using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.SeguridadXqm
{
    public class CrearDireccionDto
    {
        [MaxLength(50)]
        public string? Etiqueta { get; set; }

        [Required, MaxLength(200)]
        public string NombreReceptor { get; set; } = null!;

        /// <summary>DNI (u otro documento) del receptor. Requerido para envíos por agencia.</summary>
        [Required, MaxLength(20), MinLength(8)]
        public string DniReceptor { get; set; } = null!;

        [MaxLength(20)]
        public string? Telefono { get; set; }

        /// <summary>Código ubigeo INEI del distrito (6 dígitos).</summary>
        [Required, MaxLength(6), MinLength(6)]
        public string DistritoId { get; set; } = null!;

        [Required, MaxLength(300)]
        public string DireccionLinea { get; set; } = null!;

        [MaxLength(300)]
        public string? Referencia { get; set; }

        public bool EsPredeterminada { get; set; }
    }

    public class ActualizarDireccionDto : CrearDireccionDto { }
}
