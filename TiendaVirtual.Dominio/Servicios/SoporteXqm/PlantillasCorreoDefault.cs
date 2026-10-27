using TiendaVirtual.Comun.Enumeracion;

namespace TiendaVirtual.Dominio.Servicios.SoporteXqm
{
    /// <summary>
    /// Plantillas HTML de respaldo. El diseño es el mismo para todos los correos:
    /// hoja blanca, marca centrada y el detalle del mensaje debajo.
    /// </summary>
    public static class PlantillasCorreoDefault
    {
        public const string MarcaDiseno = "data-correo=\"tienda\"";

        private const string EstiloBase = @"
<!DOCTYPE html>
<html lang=""es"">
  <body style=""margin:0;padding:0;background:#e6e6e6;font-family:Montserrat,Segoe UI,Helvetica,Arial,sans-serif;color:#222222;"">
    <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""padding:28px 12px;background:#e6e6e6;"">
      <tr><td align=""center"">
        <table role=""presentation"" width=""640"" cellpadding=""0"" cellspacing=""0"" data-correo=""tienda"" style=""max-width:640px;width:100%;background:#ffffff;border:1px solid #e6e6e6;"">
          <tr><td style=""padding:36px 40px 8px;text-align:center;"">
            <p style=""margin:0;font-size:22px;font-weight:800;letter-spacing:0.14em;text-transform:uppercase;"">Artesanías Perú</p>
          </td></tr>
          <tr><td style=""padding:18px 40px 36px;font-size:14px;line-height:1.6;color:#333333;"">
            {CONTENIDO}
          </td></tr>
          <tr><td style=""padding:16px 40px 28px;text-align:center;font-size:12px;color:#777777;"">
            Artesanías Perú
          </td></tr>
        </table>
      </td></tr>
    </table>
  </body>
</html>";

        public static string Envolver(string contenido) =>
            EstiloBase.Replace("{CONTENIDO}", contenido.Trim(), StringComparison.Ordinal);

        public static (string asunto, string cuerpo) Obtener(PlantillaCorreo plantilla) => plantilla switch
        {
            PlantillaCorreo.CreacionUsuario => (
                "Te hemos creado una cuenta",
                Envolver(@"
            <p style=""margin:0 0 18px;text-align:center;"">Te hemos creado una cuenta en nuestra tienda.</p>
            <p style=""margin:0 0 8px;text-align:center;"">Estos son los datos de acceso:</p>
            <p style=""margin:0;text-align:center;"">Correo: <strong>{usuario}</strong></p>
            <p style=""margin:4px 0 18px;text-align:center;"">Contraseña: <strong>{clave}</strong></p>
            <p style=""margin:0;text-align:center;font-size:13px;color:#555555;"">Esta contraseña fue generada automáticamente. Puedes cambiarla cuando entres.</p>")),

            PlantillaCorreo.RecuperacionClave => (
                "Tu nueva contraseña",
                Envolver(@"
            <p style=""margin:0 0 18px;text-align:center;"">Recibimos tu solicitud para restablecer la contraseña.</p>
            <p style=""margin:0;text-align:center;"">Correo: <strong>{usuario}</strong></p>
            <p style=""margin:4px 0 18px;text-align:center;"">Nueva contraseña: <strong>{clave}</strong></p>
            <p style=""margin:0;text-align:center;font-size:13px;color:#555555;"">Al entrar te pediremos cambiarla.</p>")),

            PlantillaCorreo.NuevoPedidoVendedor => (
                "Nuevo pedido pagado {numeroPedido}",
                Envolver(@"
            <p style=""margin:0 0 16px;text-align:center;"">Hola <strong>{vendedor}</strong>, tienes un pedido pagado en tu tienda.</p>
            <p style=""margin:0 0 16px;text-align:center;background:#efefef;padding:10px 12px;font-weight:700;letter-spacing:0.06em;"">PEDIDO {numeroPedido}</p>
            <p style=""margin:0 0 8px;"">Cliente: <strong>{nombreCliente}</strong></p>
            <p style=""margin:0 0 16px;"">Total: <strong>S/ {totalPedido}</strong></p>
            {detallePedidoHtml}
            <p style=""margin:18px 0 0;text-align:center;font-size:13px;color:#555555;"">El pago ya está confirmado. Puedes preparar el envío.</p>")),

            PlantillaCorreo.PedidoPagadoCliente => (
                "Detalle pedido {numeroPedido}",
                Envolver(@"
            <p style=""margin:0 0 12px;text-align:center;""><strong>{cliente}</strong>, tu pago se realizó con éxito.</p>
            <p style=""margin:0 0 18px;text-align:center;"">Hemos recibido tu pedido y empezaremos a prepararlo.</p>
            <p style=""margin:0 0 22px;text-align:center;font-weight:700;"">¡Gracias por tu compra!</p>
            <p style=""margin:0 0 18px;text-align:center;background:#efefef;padding:10px 12px;font-weight:700;letter-spacing:0.08em;"">PEDIDO {numeroPedido}</p>
            {detallePedidoHtml}")),

            PlantillaCorreo.PedidoEnviadoCliente => (
                "Tu pedido {numeroPedido} va en camino",
                Envolver(@"
            <p style=""margin:0 0 12px;text-align:center;""><strong>{cliente}</strong>, tu pedido se ha actualizado.</p>
            <p style=""margin:0 0 18px;text-align:center;"">La tienda <strong>{nombreTienda}</strong> ya lo envió.</p>
            <p style=""margin:0 0 18px;text-align:center;background:#efefef;padding:10px 12px;font-weight:700;letter-spacing:0.08em;"">PEDIDO {numeroPedido}</p>
            <p style=""margin:0 0 6px;"">Seguimiento: <strong>{codigoSeguimiento}</strong></p>
            <p style=""margin:0 0 6px;"">Código de agencia: <strong>{codigoOrdenAgencia}</strong></p>
            <p style=""margin:0 0 14px;"">Clave de recojo: <strong>{claveRecojo}</strong></p>
            <p style=""margin:0;white-space:pre-line;"">{detallesEnvio}</p>")),

            PlantillaCorreo.VerificacionResultado => (
                "{titulo}",
                Envolver(@"
            <p style=""margin:0 0 14px;text-align:center;"">Hola <strong>{vendedor}</strong>.</p>
            <p style=""margin:0 0 8px;text-align:center;font-weight:700;"">{titulo}</p>
            <p style=""margin:0;text-align:center;"">{mensaje}</p>
            <p style=""margin:16px 0 0;text-align:center;"">Resultado: <strong>{resultado}</strong></p>")),

            PlantillaCorreo.NuevoMensajeContacto => (
                "Nuevo mensaje de contacto",
                Envolver(@"
            <p style=""margin:0 0 12px;text-align:center;"">Llegó un mensaje desde la tienda.</p>
            <p style=""margin:0;"">{mensaje}</p>")),

            PlantillaCorreo.SuscripcionPagada => (
                "Pago confirmado: plan {plan}",
                Envolver(@"
            <p style=""margin:0 0 12px;text-align:center;"">Hola <strong>{vendedor}</strong>, recibimos tu pago.</p>
            <p style=""margin:0 0 18px;text-align:center;"">Tu plan <strong>{plan}</strong> está activo.</p>
            <p style=""margin:0 0 18px;text-align:center;background:#efefef;padding:10px 12px;font-weight:700;letter-spacing:0.08em;"">PLAN {plan}</p>
            <p style=""margin:0 0 6px;"">Concepto: <strong>{concepto}</strong></p>
            <p style=""margin:0 0 6px;"">Monto pagado: <strong>{monto}</strong></p>
            <p style=""margin:0 0 6px;"">Fecha de pago: <strong>{fechaPago}</strong></p>
            <p style=""margin:0 0 18px;"">Vigente hasta: <strong>{vigenteHasta}</strong></p>
            <p style=""margin:0;text-align:center;font-size:13px;color:#555555;"">Gracias por vender con nosotros.</p>")),

            PlantillaCorreo.SuscripcionPorVencer => (
                "Tu plan {plan} vence el {venceEl}",
                Envolver(@"
            <p style=""margin:0 0 12px;text-align:center;"">Hola <strong>{vendedor}</strong>.</p>
            <p style=""margin:0 0 18px;text-align:center;"">{detalle}</p>
            <p style=""margin:0 0 18px;text-align:center;background:#efefef;padding:10px 12px;font-weight:700;letter-spacing:0.08em;"">VENCE EL {venceEl}</p>
            <p style=""margin:0 0 6px;"">Plan: <strong>{plan}</strong></p>
            <p style=""margin:0 0 18px;"">Monto de renovación: <strong>{monto}</strong></p>
            <p style=""margin:0;text-align:center;font-size:13px;color:#555555;"">Ya puedes renovar desde <strong>Mi suscripción</strong> en tu panel. El tiempo pagado se suma a tu fecha actual.</p>")),

            _ => (
                "Notificación",
                Envolver("<p style=\"margin:0;text-align:center;\">Tienes una nueva notificación.</p>"))
        };
    }
}
