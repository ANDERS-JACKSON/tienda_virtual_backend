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

        /// <summary>Único plan con meses de prueba (el de entrada). Los demás se pagan desde el inicio.</summary>
        public int? PlanPruebaId { get; set; }
        public string? PlanPruebaNombre { get; set; }
        public int MesesPrueba { get; set; }
    }
}
