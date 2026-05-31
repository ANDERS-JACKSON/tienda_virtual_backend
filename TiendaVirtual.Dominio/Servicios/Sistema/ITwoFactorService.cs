using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Dominio.Servicios.Sistema
{
    public interface ITwoFactorService
    {
        string GenerarSecreto();
        string GenerarUriOtpAuth(string secretoBase32, string correo, string emisor);
        string GenerarQrComoBase64(string secretoBase32, string correo, string emisor);
        bool ValidarCodigo(string secretoBase32, string codigo);
        string CifrarSecreto(string secretoPlano, string claveBase64);
        string DescifrarSecreto(string secretoCifrado, string claveBase64);
    }
}
