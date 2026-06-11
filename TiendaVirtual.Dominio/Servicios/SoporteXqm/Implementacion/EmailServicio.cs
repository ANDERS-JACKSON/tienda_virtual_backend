using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MimeKit;
using TiendaVirtual.Dominio.Modelo.ConfiguracionXqm;

namespace TiendaVirtual.Dominio.Servicios.SoporteXqm.Implementacion
{
    /// <summary>
    /// Servicio de email para notificaciones. Lee SMTP desde xqm_configuracion.correo.
    /// Singleton: usa IServiceScopeFactory para acceder al DbContext scoped.
    /// </summary>
    public class EmailServicio : IEmailServicio
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmailServicio> _logger;

        public EmailServicio(IServiceScopeFactory scopeFactory, ILogger<EmailServicio> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<bool> EnviarAsync(string destinatario, string nombreDestinatario,
            string asunto, string cuerpoHtml)
        {
            Correo? config;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<TiendaVirtualDbContext>();
                config = await context.Correo.AsNoTracking().FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo leer la configuración de correo desde la BD.");
                return false;
            }

            if (config == null)
            {
                _logger.LogWarning(
                    "No existe configuración de correo en xqm_configuracion.correo. " +
                    "Crea un registro con servidor_smtp, puerto, correo_electronico y contrasenia. " +
                    "Email a {Destinatario} no enviado: {Asunto}", destinatario, asunto);
                return false;
            }

            if (string.IsNullOrWhiteSpace(config.ServidorSmtp) ||
                config.Puerto == null || config.Puerto <= 0 ||
                string.IsNullOrWhiteSpace(config.CorreoElectronico) ||
                string.IsNullOrWhiteSpace(config.Contrasenia))
            {
                _logger.LogWarning(
                    "Configuración de correo incompleta en xqm_configuracion.correo " +
                    "(faltan servidor_smtp, puerto, correo_electronico o contrasenia). " +
                    "Email a {Destinatario} no enviado: {Asunto}", destinatario, asunto);
                return false;
            }

            try
            {
                using var message = new MimeMessage();
                var fromName = !string.IsNullOrWhiteSpace(config.Remitente)
                    ? config.Remitente
                    : "Artesanías";
                message.From.Add(new MailboxAddress(fromName, config.CorreoElectronico));
                message.To.Add(new MailboxAddress(nombreDestinatario, destinatario));
                message.Subject = asunto;

                var body = new BodyBuilder { HtmlBody = cuerpoHtml };
                message.Body = body.ToMessageBody();

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(config.ServidorSmtp, config.Puerto.Value, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(config.CorreoElectronico, config.Contrasenia);
                await smtp.SendAsync(message);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation(
                    "Email enviado a {Destinatario} - Asunto: {Asunto}",
                    destinatario, asunto);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error al enviar email a {Destinatario}. Asunto: {Asunto}. " +
                    "Servidor: {Servidor}:{Puerto}",
                    destinatario, asunto, config.ServidorSmtp, config.Puerto);
                return false;
            }
        }
    }
}
