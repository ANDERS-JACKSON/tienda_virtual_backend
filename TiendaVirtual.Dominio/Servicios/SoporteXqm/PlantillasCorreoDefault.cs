using TiendaVirtual.Comun.Enumeracion;

namespace TiendaVirtual.Dominio.Servicios.SoporteXqm
{
    /// <summary>
    /// Plantillas HTML de respaldo cuando la fila en xqm_configuracion.correo
    /// no tiene asunto/cuerpo para esa plantilla. Evita correos rotos o texto incorrecto en producción.
    /// </summary>
    public static class PlantillasCorreoDefault
    {
        private const string EstiloBase = @"
<!DOCTYPE html>
<html lang=""es"">
  <body style=""margin:0;padding:0;background:#f4f4f5;font-family:Segoe UI,Helvetica,Arial,sans-serif;color:#27272a;"">
    <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""padding:32px 0;"">
      <tr><td align=""center"">
        <table role=""presentation"" width=""560"" cellpadding=""0"" cellspacing=""0"" style=""background:#ffffff;border-radius:16px;overflow:hidden;border:1px solid #e4e4e7;"">
          <tr><td style=""background:#1e3a8a;padding:28px 32px;text-align:center;"">
            <h1 style=""margin:0;color:#ffffff;font-size:22px;letter-spacing:.02em;"">Artesanías Perú</h1>
          </td></tr>
          <tr><td style=""padding:32px;"">
            {CONTENIDO}
          </td></tr>
          <tr><td style=""background:#f4f4f5;padding:16px 32px;text-align:center;font-size:12px;color:#71717a;"">
            © Artesanías Perú — Hecho a mano, con historia
          </td></tr>
        </table>
      </td></tr>
    </table>
  </body>
</html>";

        public static (string asunto, string cuerpo) Obtener(PlantillaCorreo plantilla) => plantilla switch
        {
            PlantillaCorreo.NuevoPedidoVendedor => (
                "¡Pedido pagado! - {numeroPedido}",
                Envolver(@"
            <h2 style=""margin:0 0 12px;font-size:24px;color:#0f172a;"">¡Tienes un pedido pagado!</h2>
            <p style=""margin:0 0 16px;line-height:1.55;"">Hola <strong>{vendedor}</strong>, recibiste un nuevo pedido en tu tienda y <strong>el pago ya está confirmado</strong>.</p>
            <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin:20px 0;"">
              <tr>
                <td style=""padding:10px 0;border-bottom:1px solid #e4e4e7;color:#71717a;font-size:14px;"">Número de pedido</td>
                <td style=""padding:10px 0;border-bottom:1px solid #e4e4e7;color:#0f172a;font-weight:600;text-align:right;"">{numeroPedido}</td>
              </tr>
              <tr>
                <td style=""padding:10px 0;border-bottom:1px solid #e4e4e7;color:#71717a;font-size:14px;"">Cliente</td>
                <td style=""padding:10px 0;border-bottom:1px solid #e4e4e7;color:#0f172a;font-weight:600;text-align:right;"">{nombreCliente}</td>
              </tr>
              <tr>
                <td style=""padding:10px 0;color:#71717a;font-size:14px;"">Total</td>
                <td style=""padding:10px 0;color:#1e3a8a;font-weight:700;text-align:right;"">S/ {totalPedido}</td>
              </tr>
            </table>
            <div style=""margin:20px 0;padding:14px 18px;background:#ecfdf5;border-left:4px solid #059669;border-radius:8px;"">
              <p style=""margin:0;line-height:1.55;color:#065f46;""><strong>Pago confirmado.</strong> Ya puedes preparar el envío con confianza.</p>
            </div>
            <p style=""margin:24px 0 0;font-size:13px;color:#71717a;"">Ingresa a tu panel para gestionar este pedido.</p>")),

            PlantillaCorreo.PedidoPagadoCliente => (
                "Pago confirmado — pedido {numeroPedido}",
                Envolver(@"
            <h2 style=""margin:0 0 12px;font-size:24px;color:#0f172a;"">¡Pago confirmado!</h2>
            <p style=""margin:0 0 16px;line-height:1.55;"">Hola <strong>{cliente}</strong>, recibimos el pago de tu pedido correctamente.</p>
            <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin:20px 0;"">
              <tr>
                <td style=""padding:10px 0;border-bottom:1px solid #e4e4e7;color:#71717a;font-size:14px;"">Número de pedido</td>
                <td style=""padding:10px 0;border-bottom:1px solid #e4e4e7;color:#0f172a;font-weight:600;text-align:right;"">{numeroPedido}</td>
              </tr>
              <tr>
                <td style=""padding:10px 0;color:#71717a;font-size:14px;"">Total pagado</td>
                <td style=""padding:10px 0;color:#1e3a8a;font-weight:700;text-align:right;"">S/ {totalPedido}</td>
              </tr>
            </table>
            <div style=""margin:20px 0;padding:14px 18px;background:#eff6ff;border-left:4px solid #1e3a8a;border-radius:8px;"">
              <p style=""margin:0;line-height:1.55;color:#1e3a8a;"">Los artesanos ya pueden prepararlo. Te avisaremos cuando tu pedido sea enviado.</p>
            </div>
            <p style=""margin:0 0 8px;line-height:1.55;"">Puedes ver el detalle en <strong>Mis pedidos</strong>.</p>
            <p style=""margin:24px 0 0;font-size:13px;color:#71717a;"">Gracias por comprar en Artesanías Perú.</p>")),

            PlantillaCorreo.PedidoEnviadoCliente => (
                "Tu pedido {numeroPedido} está en camino",
                Envolver(@"
            <h2 style=""margin:0 0 12px;font-size:24px;color:#0f172a;"">¡Tu pedido está en camino!</h2>
            <p style=""margin:0 0 16px;line-height:1.55;"">Hola <strong>{cliente}</strong>, el artesano de <strong>{nombreTienda}</strong> ya envió tu pedido.</p>
            <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin:20px 0;"">
              <tr>
                <td style=""padding:10px 0;border-bottom:1px solid #e4e4e7;color:#71717a;font-size:14px;"">Número de pedido</td>
                <td style=""padding:10px 0;border-bottom:1px solid #e4e4e7;color:#0f172a;font-weight:600;text-align:right;"">{numeroPedido}</td>
              </tr>
              <tr>
                <td style=""padding:10px 0;border-bottom:1px solid #e4e4e7;color:#71717a;font-size:14px;"">Tienda</td>
                <td style=""padding:10px 0;border-bottom:1px solid #e4e4e7;color:#0f172a;font-weight:600;text-align:right;"">{nombreTienda}</td>
              </tr>
              <tr>
                <td style=""padding:10px 0;border-bottom:1px solid #e4e4e7;color:#71717a;font-size:14px;"">Código de seguimiento</td>
                <td style=""padding:10px 0;border-bottom:1px solid #e4e4e7;color:#1e3a8a;font-weight:700;text-align:right;font-family:Consolas,Menlo,monospace;"">{codigoSeguimiento}</td>
              </tr>
              <tr>
                <td style=""padding:10px 0;border-bottom:1px solid #e4e4e7;color:#71717a;font-size:14px;"">Código de orden (agencia)</td>
                <td style=""padding:10px 0;border-bottom:1px solid #e4e4e7;color:#0f172a;font-weight:600;text-align:right;font-family:Consolas,Menlo,monospace;"">{codigoOrdenAgencia}</td>
              </tr>
              <tr>
                <td style=""padding:10px 0;color:#71717a;font-size:14px;"">Clave de recojo</td>
                <td style=""padding:10px 0;color:#1e3a8a;font-weight:700;text-align:right;font-family:Consolas,Menlo,monospace;letter-spacing:.08em;"">{claveRecojo}</td>
              </tr>
            </table>
            <div style=""margin:16px 0;padding:14px 18px;background:#f4f4f5;border-radius:8px;"">
              <p style=""margin:0 0 6px;font-size:13px;color:#71717a;"">Detalles del envío</p>
              <p style=""margin:0;line-height:1.55;color:#27272a;white-space:pre-line;"">{detallesEnvio}</p>
            </div>
            <p style=""margin:0 0 8px;line-height:1.55;"">Guarda estos datos para reclamar tu paquete en la agencia.</p>
            <p style=""margin:24px 0 0;font-size:13px;color:#71717a;"">Si tienes preguntas, contacta al artesano desde tu panel.</p>")),

            _ => (
                "Notificación — Artesanías Perú",
                Envolver("<p style=\"margin:0;line-height:1.55;\">Tienes una nueva notificación en Artesanías Perú.</p>"))
        };

        private static string Envolver(string contenido) =>
            EstiloBase.Replace("{CONTENIDO}", contenido.Trim(), StringComparison.Ordinal);
    }
}
