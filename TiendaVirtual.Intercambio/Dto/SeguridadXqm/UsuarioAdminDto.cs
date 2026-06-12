namespace TiendaVirtual.Intercambio.Dto.SeguridadXqm
{
    public class UsuarioAdminListadoDto
    {
        public int UsuarioId { get; set; }
        public string Correo { get; set; } = null!;
        public string NombreCompleto { get; set; } = null!;
        public string? DocumentoIdentidad { get; set; }
        public List<string> Roles { get; set; } = new();
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? UltimoAcceso { get; set; }
    }

    public class UsuarioAdminDetalleDto : UsuarioAdminListadoDto
    {
        public string? Telefono { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public int? VendedorId { get; set; }
        public string? NombreTienda { get; set; }
        public int TotalOrdenes { get; set; }
        public decimal TotalGastado { get; set; }
        public bool Tiene2FA { get; set; }
    }

    public class AsignarRolDto
    {
        public int RolId { get; set; }
    }
}
