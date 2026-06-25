using System.Threading.Tasks;
using TiendaVirtual.Intercambio;

namespace TiendaVirtual.Dominio.Servicios.SeguridadXqm
{
    public class GoogleTokenPayload
    {
        public string Subject { get; set; } = null!;
        public string Email { get; set; } = null!;
        public bool EmailVerified { get; set; }
        public string? GivenName { get; set; }
        public string? FamilyName { get; set; }
        public string? Name { get; set; }
    }

    public interface IGoogleAuthServicio
    {
        Task<ResultadoOperacion<GoogleTokenPayload>> ValidarIdTokenAsync(string idToken);
    }
}
