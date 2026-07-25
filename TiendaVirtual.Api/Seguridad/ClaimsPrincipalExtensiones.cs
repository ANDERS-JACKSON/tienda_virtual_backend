using System.Security.Claims;

namespace TiendaVirtual.Api.Seguridad
{
    /// <summary>
    /// Lectura tipada y segura de claims de identidad.
    /// Centraliza el parseo a Guid para evitar IDOR por parsing laxo o claims inválidos.
    /// </summary>
    public static class ClaimsPrincipalExtensiones
    {
        public static Guid? ObtenerUsuarioId(this ClaimsPrincipal? user)
        {
            var valor = user?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user?.FindFirstValue(ClaimTypes.Name)
                ?? user?.FindFirstValue("sub");
            return Guid.TryParse(valor, out var id) && id != Guid.Empty ? id : null;
        }

        public static Guid? ObtenerPersonaId(this ClaimsPrincipal? user)
        {
            var valor = user?.FindFirstValue("personaId");
            return Guid.TryParse(valor, out var id) && id != Guid.Empty ? id : null;
        }

        public static int? ObtenerVendedorId(this ClaimsPrincipal? user)
        {
            var valor = user?.FindFirstValue("vendedorId");
            return int.TryParse(valor, out var id) && id > 0 ? id : null;
        }

        public static bool EsAdminOVerificador(this ClaimsPrincipal? user) =>
            user?.IsInRole("ADMIN") == true || user?.IsInRole("VERIFICADOR") == true;
    }
}
