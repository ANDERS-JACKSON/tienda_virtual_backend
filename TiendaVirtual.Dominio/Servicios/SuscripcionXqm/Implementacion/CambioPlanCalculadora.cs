using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;

namespace TiendaVirtual.Dominio.Servicios.SuscripcionXqm.Implementacion
{
    /// <param name="MotivoCobro">Null cuando el cambio se aplica de inmediato sin cobro.</param>
    internal sealed record CotizacionCambioPlan(
        TipoCambioPlan Tipo,
        decimal Monto,
        TipoMotivoPagoSuscripcion? MotivoCobro)
    {
        public bool RequierePago => MotivoCobro.HasValue;
    }

    /// <summary>
    /// Reglas de cambio de plan, compartidas por la cotización, el cambio y el cobro:
    /// - Downgrade (o mismo precio): inmediato, sin reembolso, conserva fechas.
    /// - Upgrade en prueba: termina la prueba y se paga el periodo completo del plan nuevo.
    /// - Upgrade en periodo pagado: se paga la diferencia prorrateada por los días restantes.
    /// - Pendiente de pago: el plan cambia sin cobro; se paga el nuevo precio al renovar.
    /// </summary>
    internal static class CambioPlanCalculadora
    {
        /// <remarks>Requiere <c>sus.Plan</c> y <c>sus.Cupon</c> cargados.</remarks>
        public static CotizacionCambioPlan Cotizar(Suscripcion sus, Plan nuevoPlan, DateTime now)
        {
            var tipo = nuevoPlan.Precio > sus.Plan.Precio ? TipoCambioPlan.Upgrade : TipoCambioPlan.Downgrade;

            if (tipo == TipoCambioPlan.Downgrade || sus.Estado == TipoEstadoSuscripcion.PendientePago)
                return new CotizacionCambioPlan(tipo, 0m, null);

            var precioNuevo = SuscripcionBeneficiosHelper.PrecioPorPeriodo(nuevoPlan, null, sus.Cupon);

            if (sus.Estado == TipoEstadoSuscripcion.EnPrueba)
                return new CotizacionCambioPlan(tipo, precioNuevo, TipoMotivoPagoSuscripcion.UpgradeDesdePrueba);

            var precioActual = SuscripcionBeneficiosHelper.PrecioPorPeriodo(
                sus.Plan, sus.PrecioPersonalizado, sus.Cupon);
            var monto = sus.PeriodoFin.HasValue
                ? SuscripcionBeneficiosHelper.CalcularProrrateoUpgrade(
                    precioActual, precioNuevo, sus.PeriodoFin.Value, sus.Plan.Periodo, now)
                : 0m;

            return monto < SuscripcionBeneficiosHelper.MontoMinimoCobro
                ? new CotizacionCambioPlan(tipo, 0m, null)
                : new CotizacionCambioPlan(tipo, monto, TipoMotivoPagoSuscripcion.UpgradeProrrateo);
        }

        /// <summary>Cambia el plan conservando fechas. El precio personalizado era del plan anterior.</summary>
        public static void AplicarPlan(Suscripcion sus, Plan nuevoPlan)
        {
            sus.PlanId = nuevoPlan.PlanId;
            sus.Plan = nuevoPlan;
            sus.PrecioPersonalizado = null;
        }
    }
}
