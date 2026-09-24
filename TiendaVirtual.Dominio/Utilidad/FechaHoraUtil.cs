using System;

namespace TiendaVirtual.Dominio.Utilidad
{
    /// <summary>
    /// PostgreSQL (Npgsql) solo acepta DateTime con Kind=Utc en columnas timestamptz.
    ///
    /// Fechas de calendario (ofertas, cupones, etc.) se interpretan en zona Perú
    /// (America/Lima, UTC−5) para que el día elegido en el formulario sea el mismo
    /// día que ve el usuario. Nunca usar medianoche UTC cruda: en Perú se ve el día anterior.
    /// </summary>
    public static class FechaHoraUtil
    {
        private static readonly Lazy<TimeZoneInfo> TzPeru = new(ResolverZonaPeru);

        private static TimeZoneInfo ResolverZonaPeru()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("America/Lima");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            }
            catch (InvalidTimeZoneException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            }
        }

        public static DateTime AUtc(DateTime value) =>
            value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            };

        /// <summary>
        /// Inicio del día calendario (Y/M/D del valor) en Perú → UTC.
        /// Ej.: 18/08 → 18/08 00:00 Lima → 18/08 05:00 UTC.
        /// </summary>
        public static DateTime InicioDiaUtc(DateTime value)
        {
            var local = new DateTime(value.Year, value.Month, value.Day, 0, 0, 0, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(local, TzPeru.Value);
        }

        /// <summary>
        /// Fin inclusivo del día calendario en Perú → UTC
        /// (un tick antes del inicio del día siguiente en Lima).
        /// </summary>
        public static DateTime FinDiaUtc(DateTime value)
        {
            var inicioSiguienteLocal = new DateTime(
                value.Year, value.Month, value.Day, 0, 0, 0, DateTimeKind.Unspecified)
                .AddDays(1);
            var inicioSiguienteUtc = TimeZoneInfo.ConvertTimeToUtc(inicioSiguienteLocal, TzPeru.Value);
            return inicioSiguienteUtc.AddTicks(-1);
        }
    }
}
