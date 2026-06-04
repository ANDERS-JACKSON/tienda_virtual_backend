using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.SeguridadXqm
{
    /// <summary>
    /// Primer cambio de contraseña tras el registro (sin JWT).
    /// El usuario usa la clave temporal recibida por correo.
    /// </summary>
    public class EstablecerContrasenaInicialDto
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        public string Correo { get; set; } = null!;

        [Required(ErrorMessage = "La contraseña del correo es obligatoria.")]
        public string ContrasenaActual { get; set; } = null!;

        [Required(ErrorMessage = "La contraseña nueva es obligatoria.")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        public string ContrasenaNueva { get; set; } = null!;
    }
}
