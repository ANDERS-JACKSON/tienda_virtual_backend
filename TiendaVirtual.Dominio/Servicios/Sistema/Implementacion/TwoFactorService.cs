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
            if (string.IsNullOrWhiteSpace(secretoBase32) || string.IsNullOrWhiteSpace(codigo))
                return false;
            try
            {
                var secretoBytes = Base32Encoding.ToBytes(secretoBase32);
                var totp = new Totp(secretoBytes);
                return totp.VerifyTotp(codigo.Trim(), out _,
                    new VerificationWindow(previous: 1, future: 1));
            }
            catch { return false; }
        }

        public string CifrarSecreto(string secretoPlano, string claveBase64)
        {
            var clave = Convert.FromBase64String(claveBase64);
            using var aes = Aes.Create();
            aes.Key = clave;
            aes.GenerateIV();
            using var encryptor = aes.CreateEncryptor();
            var datos = Encoding.UTF8.GetBytes(secretoPlano);
            var cifrado = encryptor.TransformFinalBlock(datos, 0, datos.Length);
            var resultado = new byte[aes.IV.Length + cifrado.Length];
            Buffer.BlockCopy(aes.IV, 0, resultado, 0, aes.IV.Length);
            Buffer.BlockCopy(cifrado, 0, resultado, aes.IV.Length, cifrado.Length);
            return Convert.ToBase64String(resultado);
        }

        public string DescifrarSecreto(string secretoCifrado, string claveBase64)
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
