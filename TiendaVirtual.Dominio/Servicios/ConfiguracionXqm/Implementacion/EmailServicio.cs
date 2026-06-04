using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CorreoConfig = TiendaVirtual.Dominio.Modelo.ConfiguracionXqm.Correo;

namespace TiendaVirtual.Dominio.Servicios.ConfiguracionXqm.Implementacion
{
    public class EmailServicio : IEmailServicio
    {
        private readonly TiendaVirtualDbContext _context;

        public EmailServicio(TiendaVirtualDbContext context)
        {
            _context = context;
        }

        public async Task EnviarCorreoCreacionUsuarioAsync(
            string emailDestinatario, string usuario, string claveTemporal)
        {
            var config = await ObtenerConfiguracionAsync();

            var asunto = ExigirPlantilla(
                config.AsuntoCreacionUsuario,
                "asunto_creacion_usuario");

            var cuerpoBase = ExigirPlantilla(
                config.CuerpoCreacionUsuario,
                "cuerpo_creacion_usuario");

            var cuerpo = cuerpoBase
                .Replace("{usuario}", usuario)
                .Replace("{clave}", claveTemporal);

            await EnviarAsync(config, emailDestinatario, asunto, cuerpo);
        }

        public async Task EnviarCorreoRecuperacionClaveAsync(
            string emailDestinatario, string usuario, string nuevaClave)
        {
            var config = await ObtenerConfiguracionAsync();

            var asunto = ExigirPlantilla(
                config.AsuntoRecuperacionClave,
                "asunto_recuperacion_clave");

            var cuerpoBase = ExigirPlantilla(
                config.CuerpoRecuperacionClave,
                "cuerpo_recuperacion_clave");

            var cuerpo = cuerpoBase
                .Replace("{usuario}", usuario)
                .Replace("{clave}", nuevaClave);

            await EnviarAsync(config, emailDestinatario, asunto, cuerpo);
        }

        // ─────────────────────────────────────────────────────────────
        // Helpers privados
        // ─────────────────────────────────────────────────────────────

        private async Task<CorreoConfig> ObtenerConfiguracionAsync()
        {
            // Siempre la más reciente, así el admin puede "rotar" credenciales
            // dando de alta una fila nueva.
            var config = await _context.Correo
                .AsNoTracking()
                .OrderByDescending(c => c.CorreoId)
                .FirstOrDefaultAsync();

            if (config == null)
                throw new InvalidOperationException(
                    "No hay configuración de correo registrada. " +
                    "Configure una fila en xqm_configuracion.correo antes de enviar correos.");

            if (string.IsNullOrWhiteSpace(config.ServidorSmtp))
                throw new InvalidOperationException(
                    "Debe configurar el campo 'servidor_smtp' en xqm_configuracion.correo.");

            if (string.IsNullOrWhiteSpace(config.CorreoElectronico))
                throw new InvalidOperationException(
                    "Debe configurar el campo 'correo_electronico' en xqm_configuracion.correo.");

            if (config.Puerto is null or 0)
                throw new InvalidOperationException(
                    "Debe configurar el campo 'puerto' en xqm_configuracion.correo.");

            if (string.IsNullOrWhiteSpace(config.Contrasenia))
                throw new InvalidOperationException(
                    "Debe configurar el campo 'contrasenia' (clave SMTP) en xqm_configuracion.correo.");

            if (string.IsNullOrWhiteSpace(config.Remitente))
                throw new InvalidOperationException(
                    "Debe configurar el campo 'remitente' en xqm_configuracion.correo.");

            return config;
        }

        private static string ExigirPlantilla(string? valor, string nombreCampo)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new InvalidOperationException(
                    $"Debe configurar el campo '{nombreCampo}' en xqm_configuracion.correo.");

            return valor;
        }

        private static async Task EnviarAsync(
            CorreoConfig config,
            string destinatario,
            string asunto,
            string cuerpoHtml)
        {
            if (string.IsNullOrWhiteSpace(destinatario))
                throw new ArgumentException("El correo destinatario es obligatorio.", nameof(destinatario));

            // Si en BD guardas la clave SMTP cifrada, desencripta aquí:
            //   var claveSmtp = TuHelperCripto.Desencriptar(config.Contrasenia!);
            var claveSmtp = config.Contrasenia!;

            using var mensaje = new MailMessage
            {
                From = new MailAddress(config.CorreoElectronico!, config.Remitente!),
                Subject = asunto,
                Body = cuerpoHtml,
                IsBodyHtml = true,
                BodyEncoding = System.Text.Encoding.UTF8,
                SubjectEncoding = System.Text.Encoding.UTF8
            };
            mensaje.To.Add(destinatario);

            using var smtp = new SmtpClient(config.ServidorSmtp!)
            {
                Port = config.Puerto!.Value,
                Credentials = new NetworkCredential(config.CorreoElectronico, claveSmtp),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Timeout = 20_000
            };

            try
            {
                await smtp.SendMailAsync(mensaje);
            }
            catch (SmtpException ex)
            {
                throw new InvalidOperationException(
                    $"No se pudo enviar el correo a {destinatario}: {ex.StatusCode} - {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error inesperado al enviar el correo a {destinatario}: {ex.Message}", ex);
            }
        }
    }
}
