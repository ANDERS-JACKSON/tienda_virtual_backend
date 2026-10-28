using OtpNet;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Dominio.Servicios.Sistema.Implementacion
{
    public class TwoFactorService : ITwoFactorService
    {
        public string GenerarSecreto()
        {
            var bytes = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(bytes);
        }

        public string GenerarUriOtpAuth(string secretoBase32, string correo, string emisor)
        {
            var emisorEnc = Uri.EscapeDataString(emisor);
            var cuentaEnc = Uri.EscapeDataString(correo);
            return $"otpauth://totp/{emisorEnc}:{cuentaEnc}" +
                   $"?secret={secretoBase32}&issuer={emisorEnc}&algorithm=SHA1&digits=6&period=30";
        }

        public string GenerarQrComoBase64(string secretoBase32, string correo, string emisor)
        {
            var uri = GenerarUriOtpAuth(secretoBase32, correo, emisor);
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
            var pngQr = new PngByteQRCode(qrData);
            byte[] pngBytes = pngQr.GetGraphic(20);
            return $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";
        }

        public bool ValidarCodigo(string secretoBase32, string codigo)
        {
            return ValidarCodigo(secretoBase32, codigo, out _);
        }

        public bool ValidarCodigo(string secretoBase32, string codigo, out long pasoUsado)
        {
            pasoUsado = -1;
            if (string.IsNullOrWhiteSpace(secretoBase32) || string.IsNullOrWhiteSpace(codigo))
                return false;
            try
            {
                var secretoBytes = Base32Encoding.ToBytes(secretoBase32);
                var totp = new Totp(secretoBytes);
                return totp.VerifyTotp(codigo.Trim(), out pasoUsado,
                    new VerificationWindow(previous: 1, future: 1));
            }
            catch { return false; }
        }

        /// <summary>Prefijo de secretos TOTP cifrados con AES-GCM. Lo anterior (CBC sin prefijo) se sigue leyendo.</summary>
        public const string PrefijoGcm = "gcm.v1.";

        public static bool EstaCifradoGcm(string? valor) =>
            !string.IsNullOrEmpty(valor) && valor.StartsWith(PrefijoGcm, StringComparison.Ordinal);

        public string CifrarSecreto(string secretoPlano, string claveBase64)
        {
            var clave = Convert.FromBase64String(claveBase64);
            var plano = Encoding.UTF8.GetBytes(secretoPlano);
            var nonce = new byte[12];
            RandomNumberGenerator.Fill(nonce);
            var cipher = new byte[plano.Length];
            var tag = new byte[16];
            using var gcm = new AesGcm(clave, tag.Length);
            gcm.Encrypt(nonce, plano, cipher, tag);
            var empaquetado = new byte[nonce.Length + cipher.Length + tag.Length];
            Buffer.BlockCopy(nonce, 0, empaquetado, 0, nonce.Length);
            Buffer.BlockCopy(cipher, 0, empaquetado, nonce.Length, cipher.Length);
            Buffer.BlockCopy(tag, 0, empaquetado, nonce.Length + cipher.Length, tag.Length);
            return PrefijoGcm + Convert.ToBase64String(empaquetado);
        }

        public string DescifrarSecreto(string secretoCifrado, string claveBase64)
        {
            if (EstaCifradoGcm(secretoCifrado))
                return DescifrarGcm(secretoCifrado, claveBase64);
            return DescifrarCbcLegado(secretoCifrado, claveBase64);
        }

        private static string DescifrarGcm(string secretoCifrado, string claveBase64)
        {
            var clave = Convert.FromBase64String(claveBase64);
            var empaquetado = Convert.FromBase64String(secretoCifrado[PrefijoGcm.Length..]);
            var nonce = empaquetado.AsSpan(0, 12);
            var tag = empaquetado.AsSpan(empaquetado.Length - 16, 16);
            var cipher = empaquetado.AsSpan(12, empaquetado.Length - 28);
            var plano = new byte[cipher.Length];
            using var gcm = new AesGcm(clave, 16);
            gcm.Decrypt(nonce, cipher, tag, plano);
            return Encoding.UTF8.GetString(plano);
        }

        /// <summary>Secretos TOTP guardados antes de B18 (AES-CBC, IV + ciphertext en Base64).</summary>
        private static string DescifrarCbcLegado(string secretoCifrado, string claveBase64)
        {
            var clave = Convert.FromBase64String(claveBase64);
            var datosCompletos = Convert.FromBase64String(secretoCifrado);
            using var aes = Aes.Create();
            aes.Key = clave;
            var iv = new byte[16];
            Buffer.BlockCopy(datosCompletos, 0, iv, 0, iv.Length);
            aes.IV = iv;
            var cifrado = new byte[datosCompletos.Length - iv.Length];
            Buffer.BlockCopy(datosCompletos, iv.Length, cifrado, 0, cifrado.Length);
            using var decryptor = aes.CreateDecryptor();
            var planoBytes = decryptor.TransformFinalBlock(cifrado, 0, cifrado.Length);
            return Encoding.UTF8.GetString(planoBytes);
        }
    }
}
