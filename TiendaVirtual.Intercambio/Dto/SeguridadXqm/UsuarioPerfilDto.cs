namespace TiendaVirtual.Intercambio.Dto.SeguridadXqm
{
    public class UsuarioPerfilDto
    {
        public Guid UsuarioId { get; set; }
        public string Correo { get; set; } = null!;
        public PersonaDto Persona { get; set; } = null!;
    }

    /// <summary>Datos que la persona puede corregir en su cuenta.</summary>
    public class ActualizarMisDatosPersonalesDto
    {
        public string Nombres { get; set; } = null!;
        public string? ApellidoPaterno { get; set; }
        public string? ApellidoMaterno { get; set; }
        public string? Telefono { get; set; }
        public short TipoDocumentoId { get; set; }
        public string NumeroDocumento { get; set; } = null!;
    }
}
