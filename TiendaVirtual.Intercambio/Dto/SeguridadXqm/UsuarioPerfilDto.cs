namespace TiendaVirtual.Intercambio.Dto.SeguridadXqm
{
    public class UsuarioPerfilDto
    {
        public int UsuarioId { get; set; }
        public string Correo { get; set; } = null!;
        public PersonaDto Persona { get; set; } = null!;
    }

    /// <summary>Campos editables del perfil — no incluye documento de identidad.</summary>
    public class ActualizarMisDatosPersonalesDto
    {
        public string Nombres { get; set; } = null!;
        public string? ApellidoPaterno { get; set; }
        public string? ApellidoMaterno { get; set; }
        public string? Telefono { get; set; }
    }
}
