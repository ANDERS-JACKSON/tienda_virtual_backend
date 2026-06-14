namespace TiendaVirtual.Intercambio.Dto.ConfiguracionXqm
{
    public class ConfiguracionCorreoDto
    {
        public int CorreoId { get; set; }
        public string? Remitente { get; set; }
        public string? CorreoElectronico { get; set; }
        public short? Puerto { get; set; }
        public bool ContraseniaConfigurada { get; set; }
        public string? ServidorSmtp { get; set; }
        public PlantillaCorreoItem? CreacionUsuario { get; set; }
        public PlantillaCorreoItem? RecuperacionClave { get; set; }
        public PlantillaCorreoItem? NuevoPedidoVendedor { get; set; }
        public PlantillaCorreoItem? PedidoEnviadoCliente { get; set; }
        public PlantillaCorreoItem? VerificacionResultado { get; set; }
        public PlantillaCorreoItem? NuevoMensajeContacto { get; set; }
    }

    public class PlantillaCorreoItem
    {
        public string? Asunto { get; set; }
        public string? Cuerpo { get; set; }
    }

    public class ActualizarSmtpDto
    {
        public string ServidorSmtp { get; set; } = null!;
        public short Puerto { get; set; }
        public string CorreoElectronico { get; set; } = null!;
        public string? Contrasenia { get; set; }
        public string? Remitente { get; set; }
    }

    public class ActualizarPlantillasDto
    {
        public PlantillaCorreoItem? CreacionUsuario { get; set; }
        public PlantillaCorreoItem? RecuperacionClave { get; set; }
        public PlantillaCorreoItem? NuevoPedidoVendedor { get; set; }
        public PlantillaCorreoItem? PedidoEnviadoCliente { get; set; }
        public PlantillaCorreoItem? VerificacionResultado { get; set; }
        public PlantillaCorreoItem? NuevoMensajeContacto { get; set; }
    }
}
