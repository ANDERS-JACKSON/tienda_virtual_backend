using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Dominio.Utilidad;

namespace TiendaVirtual.Dominio.Servicios.SuscripcionXqm.Implementacion
{
    internal static class SuscripcionBeneficiosHelper
    {
        public const int MesesPruebaPrimeraVez = 3;

        /// <summary>Días antes del vencimiento en que se avisa y se permite renovar anticipadamente.</summary>
        public const int DiasVentanaRenovacion = 7;

        /// <summary>Por debajo de este monto no se cobra un prorrateo (el cambio se aplica gratis).</summary>
        public const decimal MontoMinimoCobro = 1.00m;

        public static int MesesPorPeriodo(TipoPeriodoPlan periodo) =>
            periodo == TipoPeriodoPlan.Anual ? 12 : 1;

        /// <summary>Plan de entrada: el plan activo más barato. Es el único con meses de prueba.</summary>
        public static async Task<Plan?> ObtenerPlanEntradaAsync(TiendaVirtualDbContext context) =>
            await context.Planes.AsNoTracking()
                .Where(p => p.Activo)
                .OrderBy(p => p.Precio)
                .ThenBy(p => p.PlanId)
                .FirstOrDefaultAsync();

        /// <summary>Precio que paga la suscripción por periodo (precio personalizado y cupón incluidos).</summary>
        public static decimal PrecioPorPeriodo(Plan plan, decimal? precioPersonalizado, Cupon? cupon)
        {
            var precio = precioPersonalizado ?? plan.Precio;
            return cupon == null ? precio : CuponServicio.CalcularPrecioConDescuento(precio, cupon);
        }

        /// <summary>
        /// Diferencia de precio por los días que quedan del periodo pagado.
        /// La duración del ciclo se toma del calendario real que termina en <paramref name="periodoFin"/>.
        /// </summary>
        public static decimal CalcularProrrateoUpgrade(
            decimal precioActual,
            decimal precioNuevo,
            DateTime periodoFin,
            TipoPeriodoPlan periodoActual,
            DateTime now)
        {
            if (precioNuevo <= precioActual || periodoFin <= now)
                return 0m;

            var inicioCiclo = periodoFin.AddMonths(-MesesPorPeriodo(periodoActual));
            var diasCiclo = (decimal)(periodoFin - inicioCiclo).TotalDays;
            var diasRestantes = (decimal)(periodoFin - now).TotalDays;
            var monto = (precioNuevo - precioActual) * diasRestantes / diasCiclo;
            return Math.Round(monto, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Prueba vigente: se puede pagar en cualquier momento.
        /// Periodo pagado: solo dentro de la ventana de renovación.
        /// </summary>
        public static bool PuedePagarAnticipado(Suscripcion s, DateTime now) =>
            (s.Estado == TipoEstadoSuscripcion.EnPrueba &&
             s.PruebaTerminaEn.HasValue && s.PruebaTerminaEn > now) ||
            (s.Estado == TipoEstadoSuscripcion.Activa &&
             s.PeriodoFin.HasValue && s.PeriodoFin > now &&
             s.PeriodoFin.Value - now <= TimeSpan.FromDays(DiasVentanaRenovacion));

        /// <summary>Fecha en la que termina el acceso actual (prueba o periodo pagado).</summary>
        public static DateTime? FechaFinAcceso(Suscripcion s) =>
            s.Estado == TipoEstadoSuscripcion.EnPrueba ? s.PruebaTerminaEn : s.PeriodoFin;

        public static string FormatoSoles(decimal monto) =>
            "S/ " + monto.ToString("0.00", CultureInfo.InvariantCulture);

        public static string FormatoFechaPeru(DateTime utc) =>
            FechaHoraUtil.APeru(utc).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

        public static async Task<bool> EsPrimeraSuscripcionAsync(TiendaVirtualDbContext context, int vendedorId) =>
            !await context.Suscripciones.AnyAsync(s => s.VendedorId == vendedorId);

        /// <summary>Canceló o ya tuvo una suscripción vencida: sin meses gratis al volver.</summary>
        public static async Task<bool> PerdioBeneficioMesesGratisAsync(TiendaVirtualDbContext context, int vendedorId) =>
            await context.Suscripciones.AnyAsync(s =>
                s.VendedorId == vendedorId &&
                (s.Estado == TipoEstadoSuscripcion.Cancelada ||
                 s.Estado == TipoEstadoSuscripcion.Vencida));

        public static async Task<bool> ElegibleMesesGratisAsync(TiendaVirtualDbContext context, int vendedorId)
        {
            if (!await EsPrimeraSuscripcionAsync(context, vendedorId))
                return false;
            return !await PerdioBeneficioMesesGratisAsync(context, vendedorId);
        }

        public static (int MesesPruebaBase, int MesesExtraCupon, string? ErrorCuponMesesGratis) CalcularMesesGratis(
            bool elegibleMesesGratis,
            Cupon? cupon)
        {
            if (!elegibleMesesGratis)
            {
                if (cupon?.TipoDescuento == TipoDescuentoCupon.MesesGratis)
                    return (0, 0,
                        "Si cancelaste antes o ya tuviste una suscripción, no aplican meses gratis. " +
                        "Solo cupones de descuento en el pago mensual.");
                return (0, 0, null);
            }

            var mesesBase = MesesPruebaPrimeraVez;
            var mesesExtra = cupon?.TipoDescuento == TipoDescuentoCupon.MesesGratis ? cupon.MesesGratis : 0;
            return (mesesBase, mesesExtra, null);
        }

        /// <summary>
        /// Plan comercial vigente: EnPrueba o Activa dentro de fechas. Cancelada/Vencida/etc. no publican ni se muestran en catálogo.
        /// </summary>
        public static bool EsComercialmenteActiva(
            TipoEstadoSuscripcion estado,
            DateTime? periodoFin,
            DateTime? pruebaTerminaEn,
            DateTime now) =>
            (estado == TipoEstadoSuscripcion.EnPrueba &&
             pruebaTerminaEn.HasValue &&
             pruebaTerminaEn > now) ||
            (estado == TipoEstadoSuscripcion.Activa &&
             (!periodoFin.HasValue || periodoFin > now));

        public static bool PuedePublicarConEstado(
            TipoEstadoSuscripcion estado,
            DateTime? periodoFin,
            DateTime? pruebaTerminaEn,
            DateTime now) =>
            EsComercialmenteActiva(estado, periodoFin, pruebaTerminaEn, now);

        public static bool EsComercialmenteActiva(Suscripcion suscripcion, DateTime now) =>
            EsComercialmenteActiva(suscripcion.Estado, suscripcion.PeriodoFin, suscripcion.PruebaTerminaEn, now);

        /// <summary>
        /// Si la prueba o el periodo pagado ya venció, pasa a PendientePago.
        /// Devuelve true si mutó el estado (el llamador debe SaveChanges).
        /// </summary>
        public static bool AplicarVencimientoSiCorresponde(Suscripcion suscripcion, DateTime now)
        {
            if (suscripcion.Estado == TipoEstadoSuscripcion.EnPrueba &&
                suscripcion.PruebaTerminaEn.HasValue &&
                suscripcion.PruebaTerminaEn <= now)
            {
                suscripcion.Estado = TipoEstadoSuscripcion.PendientePago;
                return true;
            }

            if (suscripcion.Estado == TipoEstadoSuscripcion.Activa &&
                suscripcion.PeriodoFin.HasValue &&
                suscripcion.PeriodoFin <= now)
            {
                suscripcion.Estado = TipoEstadoSuscripcion.PendientePago;
                return true;
            }

            return false;
        }

        /// <summary>True cuando el vendedor debe pagar para recuperar/activar el acceso.</summary>
        public static bool RequierePagoAhora(Suscripcion suscripcion, DateTime now)
        {
            if (suscripcion.Estado == TipoEstadoSuscripcion.PendientePago)
                return true;

            if (suscripcion.Estado == TipoEstadoSuscripcion.EnPrueba &&
                (!suscripcion.PruebaTerminaEn.HasValue || suscripcion.PruebaTerminaEn <= now))
                return true;

            if (suscripcion.Estado == TipoEstadoSuscripcion.Activa &&
                suscripcion.PeriodoFin.HasValue &&
                suscripcion.PeriodoFin <= now)
                return true;

            return false;
        }
    }
}
