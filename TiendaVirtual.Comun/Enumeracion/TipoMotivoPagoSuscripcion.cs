using System.ComponentModel;

namespace TiendaVirtual.Comun.Enumeracion
{
    /// <summary>Qué compra el vendedor con un cobro de suscripción.</summary>
    public enum TipoMotivoPagoSuscripcion : short
    {
        /// <summary>Periodo siguiente del plan actual (pendiente o renovación anticipada).</summary>
        [Description("RENOVACION")] Renovacion = 1,

        /// <summary>Pago durante la prueba: el periodo pagado se suma al final de la prueba.</summary>
        [Description("ANTICIPADO_PRUEBA")] AnticipadoPrueba = 2,

        /// <summary>Diferencia prorrateada para subir de plan en un periodo pagado.</summary>
        [Description("UPGRADE_PRORRATEO")] UpgradeProrrateo = 3,

        /// <summary>Subir de plan durante la prueba: termina la prueba y se paga el periodo completo.</summary>
        [Description("UPGRADE_DESDE_PRUEBA")] UpgradeDesdePrueba = 4
    }
}
