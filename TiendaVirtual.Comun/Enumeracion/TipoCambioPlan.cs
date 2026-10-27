using System.ComponentModel;

namespace TiendaVirtual.Comun.Enumeracion
{
    public enum TipoCambioPlan : short
    {
        [Description("UPGRADE")] Upgrade = 1,

        /// <summary>Plan de igual o menor precio: se aplica sin cobro.</summary>
        [Description("DOWNGRADE")] Downgrade = 2
    }
}
