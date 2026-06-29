namespace TiendaVirtual.Comun.Utilidades
{
    /// <summary>Genera resúmenes de texto con truncado en límite de palabra.</summary>
    public static class TextoResumenHelper
    {
        public const int LongitudResumenHistorias = 180;

        public static (string Resumen, bool FueTruncado) CrearResumen(
            string? texto,
            int maxLongitud = LongitudResumenHistorias)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return (string.Empty, false);

            var normalizado = texto.Trim();
            if (normalizado.Length <= maxLongitud)
                return (normalizado, false);

            var corte = normalizado.LastIndexOf(' ', maxLongitud);
            if (corte <= 0)
                corte = maxLongitud;

            var resumen = normalizado[..corte].TrimEnd();
            return (resumen + "…", true);
        }
    }
}
