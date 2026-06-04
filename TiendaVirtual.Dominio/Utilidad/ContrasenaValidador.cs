using System.Text.RegularExpressions;
using TiendaVirtual.Intercambio;

namespace TiendaVirtual.Dominio.Utilidad
{
    /// <summary>
    /// Reglas de contraseña segura: mínimo 8 caracteres, mayúscula, minúscula y símbolo.
    /// </summary>
    public static class ContrasenaValidador
    {
        private const int LongitudMinima = 8;

        private static readonly Regex TieneMayuscula = new(@"[A-Z]", RegexOptions.Compiled);
        private static readonly Regex TieneMinuscula = new(@"[a-z]", RegexOptions.Compiled);
        private static readonly Regex TieneSimbolo = new(@"[^A-Za-z0-9]", RegexOptions.Compiled);

        public static ResultadoOperacion<bool> Validar(string? contrasena)
        {
            if (string.IsNullOrWhiteSpace(contrasena))
                return ResultadoOperacion<bool>.SetError("La contraseña es obligatoria.");

            if (contrasena.Length < LongitudMinima)
                return ResultadoOperacion<bool>.SetError(
                    $"La contraseña debe tener al menos {LongitudMinima} caracteres.");

            if (!TieneMayuscula.IsMatch(contrasena))
                return ResultadoOperacion<bool>.SetError(
                    "La contraseña debe incluir al menos una letra mayúscula.");

            if (!TieneMinuscula.IsMatch(contrasena))
                return ResultadoOperacion<bool>.SetError(
                    "La contraseña debe incluir al menos una letra minúscula.");

            if (!TieneSimbolo.IsMatch(contrasena))
                return ResultadoOperacion<bool>.SetError(
                    "La contraseña debe incluir al menos un símbolo (ej. ! @ # $ %).");

            return ResultadoOperacion<bool>.SetExito(true);
        }
    }
}
