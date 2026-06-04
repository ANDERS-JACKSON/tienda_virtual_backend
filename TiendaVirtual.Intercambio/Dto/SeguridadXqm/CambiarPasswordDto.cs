using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.SeguridadXqm
{
    /// <summary>
    /// Cambio de contraseña con sesión iniciada (perfil o tras login obligatorio).
    /// El usuario se identifica por el token JWT.
    /// </summary>
    public class CambiarPasswordDto
    {
        [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
        public string ContrasenaActual { get; set; } = null!;

        [Required(ErrorMessage = "La contraseña nueva es obligatoria.")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        public string ContrasenaNueva { get; set; } = null!;
    }
}
