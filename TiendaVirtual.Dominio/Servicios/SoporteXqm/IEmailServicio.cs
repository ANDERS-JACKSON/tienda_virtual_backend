using System.Collections.Generic;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;

namespace TiendaVirtual.Dominio.Servicios.SoporteXqm
{
    public interface IEmailServicio
    {
        /// <summary>
        /// Envía un correo usando una plantilla configurada en
        /// xqm_configuracion.correo. Sustituye los placeholders del
        /// formato {nombrePlaceholder} en el asunto y el cuerpo.
        ///
        /// Retorna false si falta config SMTP o si la plantilla no está
        /// configurada. NO lanza excepciones.
        /// </summary>
        Task<bool> EnviarAsync(
            string destinatario,
            string nombreDestinatario,
            PlantillaCorreo plantilla,
            Dictionary<string, string> placeholders);

        /// <summary>
        /// Envía un correo de prueba SMTP sin depender de plantillas configuradas.
        /// </summary>
        Task<bool> EnviarPruebaAsync(string destinatario, string nombreDestinatario);
    }
}
