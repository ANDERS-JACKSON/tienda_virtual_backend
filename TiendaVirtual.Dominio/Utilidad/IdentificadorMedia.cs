using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TiendaVirtual.Dominio.Utilidad
{
    /// <summary>
    /// Valida los identificadores de imagen/archivo que el cliente intenta persistir.
    /// Acepta public_id de Cloudinary o URL https de res.cloudinary.com.
    /// Rechaza javascript:/data:/http plano y cualquier host ajeno.
    /// </summary>
    public static class IdentificadorMedia
    {
        public const int LongitudMaxima = 500;
        public const string MensajeError = "El archivo o imagen no es válido.";

        private static readonly Regex PublicIdValido = new(
            @"^[A-Za-z0-9][A-Za-z0-9_./-]*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static bool EsValido(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return false;

            var v = valor.Trim();
            if (v.Length > LongitudMaxima)
                return false;
            if (v.Contains("..", StringComparison.Ordinal))
                return false;
            if (v.IndexOfAny([' ', '<', '>', '"', '\'', '\n', '\r', '\t']) >= 0)
                return false;

            if (v.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)
                || v.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
                || v.StartsWith("vbscript:", StringComparison.OrdinalIgnoreCase)
                || v.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
                return false;

            if (v.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                if (!Uri.TryCreate(v, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                    return false;
                return uri.Host.Equals("res.cloudinary.com", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.EndsWith(".cloudinary.com", StringComparison.OrdinalIgnoreCase);
            }

            if (v.Contains(':', StringComparison.Ordinal))
                return false;

            return PublicIdValido.IsMatch(v);
        }

        public static bool EsValidoOVacio(string? valor) =>
            string.IsNullOrWhiteSpace(valor) || EsValido(valor);

        /// <summary>
        /// URL https para un &lt;img&gt; de correo. public_id → Cloudinary; URL https se deja igual.
        /// Null si no hay identificador válido o falta el cloud.
        /// </summary>
        public static string? UrlEntregaImagen(string? identificador, string? cloudName, int lado = 128)
        {
            if (string.IsNullOrWhiteSpace(identificador) || !EsValido(identificador))
                return null;

            var v = identificador.Trim();
            if (v.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return v;

            if (string.IsNullOrWhiteSpace(cloudName))
                return null;

            var cloud = cloudName.Trim();
            if (cloud.Length == 0 || cloud.IndexOfAny(['/', '\\', ' ', '<', '>', '"']) >= 0)
                return null;

            var px = lado < 32 ? 32 : (lado > 800 ? 800 : lado);
            return $"https://res.cloudinary.com/{cloud}/image/upload/w_{px},h_{px},c_fill,q_auto,f_auto/{v}";
        }

        /// <summary>
        /// URL https para un PDF de patrón (raw/upload). Null si falta cloud o el id no es válido.
        /// </summary>
        public static string? UrlEntregaArchivo(string? identificador, string? cloudName)
        {
            if (string.IsNullOrWhiteSpace(identificador) || !EsValido(identificador))
                return null;

            var v = identificador.Trim();
            if (v.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return v;

            if (string.IsNullOrWhiteSpace(cloudName))
                return null;

            var cloud = cloudName.Trim();
            if (cloud.Length == 0 || cloud.IndexOfAny(['/', '\\', ' ', '<', '>', '"']) >= 0)
                return null;

            return $"https://res.cloudinary.com/{cloud}/raw/upload/{v}";
        }

        public static bool ListaValida(IEnumerable<string>? items) =>
            items == null || items.All(EsValido);

        /// <summary>
        /// FotosProductos llega como JSON de public_ids (FormularioVerificacion).
        /// Vacío es válido; JSON malformado o ítems inválidos, no.
        /// </summary>
        public static bool JsonListaValida(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return true;

            try
            {
                var lista = JsonSerializer.Deserialize<List<string>>(json);
                return ListaValida(lista);
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
