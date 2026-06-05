namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    /// <summary>Reglas de contratación/reactivación para el vendedor.</summary>
    public class SuscripcionElegibilidadDto
    {
        public bool ElegibleMesesGratis { get; set; }
        public bool PuedeContratarNueva { get; set; }
        public bool RequierePagoInmediato { get; set; }
        public bool TieneCanceladaConAcceso { get; set; }
        public string Mensaje { get; set; } = null!;
    }
}
