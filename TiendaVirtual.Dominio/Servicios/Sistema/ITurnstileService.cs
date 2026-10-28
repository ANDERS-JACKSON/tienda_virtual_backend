using System.Threading.Tasks;

namespace TiendaVirtual.Dominio.Servicios.Sistema
{
    /// <summary>
    /// Validación server-side de Cloudflare Turnstile (anti-bot) mediante una
    /// llamada HTTP directa a la API de Cloudflare, sin paquete NuGet adicional.
    /// Totalmente apagable por configuración (Turnstile:Activo=false), para que
    /// el entorno de desarrollo siga funcionando sin captcha.
    /// </summary>
    public interface ITurnstileService
    {
        /// <summary>True si Turnstile:Activo=true y hay una SecretKey configurada.</summary>
        bool Activo { get; }

        /// <summary>
        /// Valida el token del widget de Turnstile. Si <see cref="Activo"/> es false,
        /// siempre devuelve true (no bloquea nada en desarrollo).
        /// </summary>
        Task<bool> ValidarAsync(string? token, string? direccionIp);
    }
}
