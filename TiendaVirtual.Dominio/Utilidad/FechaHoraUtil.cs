using System;

namespace TiendaVirtual.Dominio.Utilidad
{
    /// <summary>
    /// PostgreSQL (Npgsql) solo acepta DateTime con Kind=Utc en columnas timestamptz.
    /// </summary>
    public static class FechaHoraUtil
    {
        public static DateTime AUtc(DateTime value) =>
            value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            };

        /// <summary>Inicio del día en UTC (p. ej. oferta que empieza el 01/06).</summary>
        public static DateTime InicioDiaUtc(DateTime value) =>
            DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

        /// <summary>Último instante del día en UTC (oferta vigente todo el día de fin).</summary>
        public static DateTime FinDiaUtc(DateTime value) =>
            DateTime.SpecifyKind(value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
    }
}
