using OtpNet;
using TiendaVirtual.Dominio.Servicios.Sistema.Implementacion;
using Xunit;

namespace TiendaVirtual.Pruebas.Seguridad
{
    /// <summary>
    /// A3: la validación de TOTP debe exponer el "paso" (ventana de 30s) que
    /// coincidió, para que AutenticacionServicio pueda rechazar un código reutilizado.
    /// </summary>
    public class TwoFactorServiceTests
    {
        [Fact]
        public void ValidarCodigo_DevuelveElMismoPasoParaElMismoCodigoYMomento()
        {
            var servicio = new TwoFactorService();
            var secreto = servicio.GenerarSecreto();

            var secretoBytes = Base32Encoding.ToBytes(secreto);
            var totp = new Totp(secretoBytes);
            var codigoActual = totp.ComputeTotp();

            Assert.True(servicio.ValidarCodigo(secreto, codigoActual, out var paso1));
            Assert.True(servicio.ValidarCodigo(secreto, codigoActual, out var paso2));

            // Mismo código, mismo paso: esto es lo que permite detectar un replay
            // (AutenticacionServicio compara este valor contra Usuario.TotpUltimoPaso).
            Assert.Equal(paso1, paso2);
        }

        [Fact]
        public void ValidarCodigo_RechazaCodigoVacioOInvalido()
        {
            var servicio = new TwoFactorService();
            var secreto = servicio.GenerarSecreto();

            Assert.False(servicio.ValidarCodigo(secreto, ""));
            Assert.False(servicio.ValidarCodigo(secreto, "000000"));
        }

        [Fact]
        public void CifrarYDescifrarSecreto_DevuelveElValorOriginal()
        {
            var servicio = new TwoFactorService();
            var clave = "kF7PXJWnLEm/HVtpUcvbqf2DMRC9YwgZkXxvR3sZ8ck=";
            var secreto = servicio.GenerarSecreto();

            var cifrado = servicio.CifrarSecreto(secreto, clave);
            var descifrado = servicio.DescifrarSecreto(cifrado, clave);

            Assert.Equal(secreto, descifrado);
            Assert.NotEqual(secreto, cifrado);
            Assert.StartsWith(TwoFactorService.PrefijoGcm, cifrado);
        }

        [Fact]
        public void DescifrarSecreto_SigueLeyendoElCbcLegado()
        {
            var servicio = new TwoFactorService();
            const string clave = "kF7PXJWnLEm/HVtpUcvbqf2DMRC9YwgZkXxvR3sZ8ck=";
            const string secreto = "JBSWY3DPEHPK3PXP";

            var claveBytes = Convert.FromBase64String(clave);
            using var aes = System.Security.Cryptography.Aes.Create();
            aes.Key = claveBytes;
            aes.GenerateIV();
            using var encryptor = aes.CreateEncryptor();
            var datos = System.Text.Encoding.UTF8.GetBytes(secreto);
            var cifrado = encryptor.TransformFinalBlock(datos, 0, datos.Length);
            var empaquetado = new byte[aes.IV.Length + cifrado.Length];
            Buffer.BlockCopy(aes.IV, 0, empaquetado, 0, aes.IV.Length);
            Buffer.BlockCopy(cifrado, 0, empaquetado, aes.IV.Length, cifrado.Length);
            var legado = Convert.ToBase64String(empaquetado);

            Assert.False(TwoFactorService.EstaCifradoGcm(legado));
            Assert.Equal(secreto, servicio.DescifrarSecreto(legado, clave));
        }
    }
}
