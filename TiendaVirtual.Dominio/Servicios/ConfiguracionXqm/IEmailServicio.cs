using System.Threading.Tasks;

namespace TiendaVirtual.Dominio.Servicios.ConfiguracionXqm
{
    /// <summary>
    /// Envío transaccional de correos. La configuración SMTP y las plantillas
    /// se leen siempre de la última fila de <c>xqm_configuracion.correo</c>.
    /// </summary>
    public interface IEmailServicio
    {
        /// <summary>
        /// Envía la contraseña temporal generada en el alta de un usuario.
        /// Reemplaza <c>{usuario}</c> y <c>{clave}</c> en la plantilla.
        /// </summary>
        Task EnviarCorreoCreacionUsuarioAsync(
            string emailDestinatario, string usuario, string claveTemporal);

        /// <summary>
        /// Envía la nueva contraseña generada al recuperar acceso por correo.
        /// Reemplaza <c>{usuario}</c> y <c>{clave}</c> en la plantilla.
        /// </summary>
        Task EnviarCorreoRecuperacionClaveAsync(
            string emailDestinatario, string usuario, string nuevaClave);
    }
}
