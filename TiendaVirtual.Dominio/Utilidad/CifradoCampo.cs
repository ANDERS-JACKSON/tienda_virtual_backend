using System;
using System.Security.Cryptography;
using System.Text;

namespace TiendaVirtual.Dominio.Utilidad
{
    /// <summary>
    /// AES-CBC con prefijo para campos sensibles en BD (hoy: contraseña SMTP).
    /// Si el valor no trae el prefijo, se trata como texto plano legado: así el
    /// SMTP actual sigue funcionando hasta que el admin vuelva a guardar la clave.
    /// Reusa TwoFactor:ClaveCifrado (misma clave de 32 bytes en Base64).
    /// </summary>
    public static class CifradoCampo
    {
        public const string Prefijo = "enc.v1.";

        public static bool EstaCifrado(string? valor) =>
            !string.IsNullOrEmpty(valor)
            && valor.StartsWith(Prefijo, StringComparison.Ordinal);

        public static string Cifrar(string plano, string claveBase64)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(plano);
            ArgumentException.ThrowIfNullOrWhiteSpace(claveBase64);

            var clave = Convert.FromBase64String(claveBase64);
            using var aes = Aes.Create();
            aes.Key = clave;
            aes.GenerateIV();
            using var encryptor = aes.CreateEncryptor();
            var datos = Encoding.UTF8.GetBytes(plano);
            var cifrado = encryptor.TransformFinalBlock(datos, 0, datos.Length);
            var resultado = new byte[aes.IV.Length + cifrado.Length];
            Buffer.BlockCopy(aes.IV, 0, resultado, 0, aes.IV.Length);
            Buffer.BlockCopy(cifrado, 0, resultado, aes.IV.Length, cifrado.Length);
            return Prefijo + Convert.ToBase64String(resultado);
        }

        /// <summary>
        /// Devuelve el plano. Si no está cifrado, lo deja tal cual (filas antiguas).
        /// Si está cifrado y no hay clave, lanza: no se puede autenticar SMTP a ciegas.
        /// </summary>
        public static string Descifrar(string valor, string? claveBase64)
        {
            if (string.IsNullOrEmpty(valor) || !EstaCifrado(valor))
                return valor;

            if (string.IsNullOrWhiteSpace(claveBase64))
                throw new InvalidOperationException(
                    "Hay un secreto cifrado en BD pero TwoFactor:ClaveCifrado no está configurada.");

            var clave = Convert.FromBase64String(claveBase64);
            var payload = Convert.FromBase64String(valor[Prefijo.Length..]);
            using var aes = Aes.Create();
            aes.Key = clave;
            var iv = new byte[16];
            Buffer.BlockCopy(payload, 0, iv, 0, iv.Length);
            aes.IV = iv;
            var cifrado = new byte[payload.Length - iv.Length];
            Buffer.BlockCopy(payload, iv.Length, cifrado, 0, cifrado.Length);
            using var decryptor = aes.CreateDecryptor();
            var plano = decryptor.TransformFinalBlock(cifrado, 0, cifrado.Length);
            return Encoding.UTF8.GetString(plano);
        }
    }
}
