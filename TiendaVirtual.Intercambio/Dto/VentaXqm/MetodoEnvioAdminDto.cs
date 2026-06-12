namespace TiendaVirtual.Intercambio.Dto.VentaXqm
{
    public class MetodoEnvioAdminDto
    {
        public int MetodoEnvioId { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public decimal CostoBase { get; set; }
        public int DiasEntregaMin { get; set; }
        public int DiasEntregaMax { get; set; }
        public bool Activo { get; set; }
    }

    public class CrearMetodoEnvioDto
    {
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public decimal CostoBase { get; set; }
        public int DiasEntregaMin { get; set; }
        public int DiasEntregaMax { get; set; }
    }

    public class ActualizarMetodoEnvioDto : CrearMetodoEnvioDto
    {
        public bool Activo { get; set; }
    }
}
