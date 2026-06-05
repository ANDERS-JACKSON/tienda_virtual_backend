using System.ComponentModel;

namespace TiendaVirtual.Comun.Enumeracion
{
    public enum TipoDescuentoCupon : short
    {
        [Description("PORCENTAJE")] Porcentaje = 1,
        [Description("MONTO_FIJO")] MontoFijo = 2,
        [Description("MESES_GRATIS")] MesesGratis = 3
    }
}
