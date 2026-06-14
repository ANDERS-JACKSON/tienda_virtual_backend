using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MimeKit;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.ConfiguracionDB;
using TiendaVirtual.Dominio.Modelo.ConfiguracionXqm;

namespace TiendaVirtual.Dominio.Servicios.SoporteXqm.Implementacion
{
    /// <summary>
    /// Servicio de email. SINGLETON: usa IServiceScopeFactory para acceder
    /// al DbContext, así puede ejecutarse en tareas fire-and-forget que
    /// sobreviven al request HTTP.
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
            PlantillaCorreo plantilla, Dictionary<string, string> placeholders)
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
                _logger.LogError(ex,
                    "No se pudo leer la configuración de correo. Plantilla {Plantilla}, Destinatario {Destinatario}.",
                    plantilla, destinatario);
                return false;
            }

            if (config == null)
            {
                _logger.LogWarning(
                    "⚠️  Configuración de correo NO EXISTE en xqm_configuracion.correo. " +
                    "Crea el registro con servidor_smtp, puerto, correo_electronico y contrasenia. " +
                    "Email a {Destinatario} (plantilla {Plantilla}) no enviado.",
                    destinatario, plantilla);
                return false;
            }

            if (string.IsNullOrWhiteSpace(config.ServidorSmtp) ||
                config.Puerto == null || config.Puerto <= 0 ||
                string.IsNullOrWhiteSpace(config.CorreoElectronico) ||
                string.IsNullOrWhiteSpace(config.Contrasenia))
            {
                _logger.LogWarning(
                    "⚠️  Configuración SMTP incompleta. Faltan datos en xqm_configuracion.correo " +
                    "(servidor_smtp, puerto, correo_electronico o contrasenia). " +
                    "Email a {Destinatario} (plantilla {Plantilla}) no enviado.",
                    destinatario, plantilla);
                return false;
            }

            var (asuntoRaw, cuerpoRaw) = ObtenerPlantilla(config, plantilla);

            if (string.IsNullOrWhiteSpace(asuntoRaw) || string.IsNullOrWhiteSpace(cuerpoRaw))
            {
                _logger.LogWarning(
                    "⚠️  Plantilla {Plantilla} NO está configurada en xqm_configuracion.correo " +
                    "(asunto o cuerpo vacíos). Configurar las columnas correspondientes. " +
                    "Email a {Destinatario} no enviado.",
                    plantilla, destinatario);
                return false;
            }

            var asunto = SustituirPlaceholders(asuntoRaw, placeholders);
            var cuerpoHtml = SustituirPlaceholders(cuerpoRaw, placeholders);
            return await EnviarMensajeAsync(config, destinatario, nombreDestinatario, asunto, cuerpoHtml,
                $"plantilla {plantilla}");
        }

        public async Task<bool> EnviarPruebaAsync(string destinatario, string nombreDestinatario)
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
                _logger.LogError(ex, "No se pudo leer la configuración de correo para prueba SMTP.");
                return false;
            }

            if (config == null || !EsConfigSmtpValida(config))
            {
                _logger.LogWarning("⚠️  Configuración SMTP incompleta. Email de prueba no enviado.");
                return false;
            }

            const string asunto = "Prueba de configuración SMTP — Artesanías Perú";
            var cuerpo = $@"<p>Hola {nombreDestinatario},</p>
<p>Esta es una <strong>prueba de configuración SMTP</strong> del panel de administración.</p>
<p>Si recibes este correo, la configuración es correcta.</p>
<p><em>Artesanías Perú</em></p>";

            return await EnviarMensajeAsync(config, destinatario, nombreDestinatario, asunto, cuerpo, "prueba SMTP");
        }

        public async Task<bool> EnviarHtmlAsync(
            string destinatario, string nombreDestinatario, string asunto, string cuerpoHtml)
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
                _logger.LogError(ex, "No se pudo leer SMTP para email HTML a {Destinatario}.", destinatario);
                return false;
            }

            if (config == null || !EsConfigSmtpValida(config))
            {
                _logger.LogWarning("⚠️  SMTP incompleto. Email HTML a {Destinatario} no enviado.", destinatario);
                return false;
            }

            return await EnviarMensajeAsync(config, destinatario, nombreDestinatario, asunto, cuerpoHtml, "HTML libre");
        }

        private async Task<bool> EnviarMensajeAsync(
            Correo config, string destinatario, string nombreDestinatario,
            string asunto, string cuerpoHtml, string contextoLog)
        {
            try
            {
                using var message = new MimeMessage();
                var fromName = !string.IsNullOrWhiteSpace(config.Remitente)
                    ? config.Remitente
                    : "Artesanías Perú";
                message.From.Add(new MailboxAddress(fromName, config.CorreoElectronico));
                message.To.Add(new MailboxAddress(nombreDestinatario, destinatario));
                message.Subject = asunto;
                message.Body = new BodyBuilder { HtmlBody = cuerpoHtml }.ToMessageBody();

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(config.ServidorSmtp, config.Puerto!.Value, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(config.CorreoElectronico, config.Contrasenia);
                await smtp.SendAsync(message);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation(
                    "✉️  Email enviado a {Destinatario} ({Contexto}). Asunto: {Asunto}",
                    destinatario, contextoLog, asunto);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error SMTP al enviar email a {Destinatario} ({Contexto}). Servidor {Servidor}:{Puerto}.",
                    destinatario, contextoLog, config.ServidorSmtp, config.Puerto);
                return false;
            }
        }

        private static bool EsConfigSmtpValida(Correo config) =>
            !string.IsNullOrWhiteSpace(config.ServidorSmtp) &&
            config.Puerto is > 0 &&
            !string.IsNullOrWhiteSpace(config.CorreoElectronico) &&
            !string.IsNullOrWhiteSpace(config.Contrasenia);

        private static (string? asunto, string? cuerpo) ObtenerPlantilla(Correo c, PlantillaCorreo p) => p switch
        {
            PlantillaCorreo.CreacionUsuario => (c.AsuntoCreacionUsuario, c.CuerpoCreacionUsuario),
            PlantillaCorreo.RecuperacionClave => (c.AsuntoRecuperacionClave, c.CuerpoRecuperacionClave),
            PlantillaCorreo.NuevoPedidoVendedor => (c.AsuntoNuevoPedidoVendedor, c.CuerpoNuevoPedidoVendedor),
            PlantillaCorreo.PedidoEnviadoCliente => (c.AsuntoPedidoEnviadoCliente, c.CuerpoPedidoEnviadoCliente),
            PlantillaCorreo.VerificacionResultado => (c.AsuntoVerificacionResultado, c.CuerpoVerificacionResultado),
            PlantillaCorreo.NuevoMensajeContacto => (c.AsuntoNuevoMensajeContacto, c.CuerpoNuevoMensajeContacto),
            _ => (null, null)
        };

        private static string SustituirPlaceholders(string texto, Dictionary<string, string> placeholders)
        {
            if (placeholders == null || placeholders.Count == 0) return texto;
            foreach (var kv in placeholders)
                texto = texto.Replace("{" + kv.Key + "}", kv.Value ?? string.Empty);
            return texto;
        }
    }
}
