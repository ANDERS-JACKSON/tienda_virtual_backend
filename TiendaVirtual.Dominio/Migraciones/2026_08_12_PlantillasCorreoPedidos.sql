-- ============================================================================
--  Migración: plantillas de correo de pedidos (pago confirmado + envío)
--  Fecha: 2026-08-12
--
--  Qué hace:
--    1) Agrega columnas para el correo profesional al cliente cuando el pago se confirma.
--    2) Actualiza el cuerpo del vendedor (ya no dice "te avisaremos cuando se confirme").
--    3) Actualiza el correo de envío para incluir seguimiento, orden de agencia y clave.
--
--  Ejecutar en PostgreSQL contra la BD de la tienda.
-- ============================================================================

ALTER TABLE xqm_configuracion.correo
    ADD COLUMN IF NOT EXISTS asunto_pedido_pagado_cliente varchar(1000) NULL,
    ADD COLUMN IF NOT EXISTS cuerpo_pedido_pagado_cliente text NULL;

-- Ampliar cuerpos si aún son varchar cortos (idempotente si ya son text)
DO $$
BEGIN
    ALTER TABLE xqm_configuracion.correo
        ALTER COLUMN cuerpo_nuevo_pedido_vendedor TYPE text,
        ALTER COLUMN cuerpo_pedido_enviado_cliente TYPE text,
        ALTER COLUMN cuerpo_pedido_pagado_cliente TYPE text;
EXCEPTION
    WHEN others THEN
        RAISE NOTICE 'Alter cuerpos a text omitido: %', SQLERRM;
END $$;

UPDATE xqm_configuracion.correo
SET
    asunto_nuevo_pedido_vendedor = '¡Pedido pagado! - {numeroPedido}',
    cuerpo_nuevo_pedido_vendedor = $html$<!DOCTYPE html>
<html lang="es">
  <body style="margin:0;padding:0;background:#f4f4f5;font-family:Segoe UI,Helvetica,Arial,sans-serif;color:#27272a;">
    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="padding:32px 0;">
      <tr><td align="center">
        <table role="presentation" width="560" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:16px;overflow:hidden;border:1px solid #e4e4e7;">
          <tr><td style="background:#1e3a8a;padding:28px 32px;text-align:center;">
            <h1 style="margin:0;color:#ffffff;font-size:22px;letter-spacing:.02em;">Artesanías Perú</h1>
          </td></tr>
          <tr><td style="padding:32px;">
            <h2 style="margin:0 0 12px;font-size:24px;color:#0f172a;">¡Tienes un pedido pagado!</h2>
            <p style="margin:0 0 16px;line-height:1.55;">Hola <strong>{vendedor}</strong>, recibiste un nuevo pedido en tu tienda y <strong>el pago ya está confirmado</strong>.</p>
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:20px 0;">
              <tr>
                <td style="padding:10px 0;border-bottom:1px solid #e4e4e7;color:#71717a;font-size:14px;">Número de pedido</td>
                <td style="padding:10px 0;border-bottom:1px solid #e4e4e7;color:#0f172a;font-weight:600;text-align:right;">{numeroPedido}</td>
              </tr>
              <tr>
                <td style="padding:10px 0;border-bottom:1px solid #e4e4e7;color:#71717a;font-size:14px;">Cliente</td>
                <td style="padding:10px 0;border-bottom:1px solid #e4e4e7;color:#0f172a;font-weight:600;text-align:right;">{nombreCliente}</td>
              </tr>
              <tr>
                <td style="padding:10px 0;color:#71717a;font-size:14px;">Total</td>
                <td style="padding:10px 0;color:#1e3a8a;font-weight:700;text-align:right;">S/ {totalPedido}</td>
              </tr>
            </table>
            <div style="margin:20px 0;padding:14px 18px;background:#ecfdf5;border-left:4px solid #059669;border-radius:8px;">
              <p style="margin:0;line-height:1.55;color:#065f46;"><strong>Pago confirmado.</strong> Ya puedes preparar el envío con confianza.</p>
            </div>
            <p style="margin:24px 0 0;font-size:13px;color:#71717a;">Ingresa a tu panel para gestionar este pedido.</p>
          </td></tr>
          <tr><td style="background:#f4f4f5;padding:16px 32px;text-align:center;font-size:12px;color:#71717a;">
            © Artesanías Perú — Hecho a mano, con historia
          </td></tr>
        </table>
      </td></tr>
    </table>
  </body>
</html>$html$,

    asunto_pedido_pagado_cliente = 'Pago confirmado — pedido {numeroPedido}',
    cuerpo_pedido_pagado_cliente = $html$<!DOCTYPE html>
<html lang="es">
  <body style="margin:0;padding:0;background:#f4f4f5;font-family:Segoe UI,Helvetica,Arial,sans-serif;color:#27272a;">
    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="padding:32px 0;">
      <tr><td align="center">
        <table role="presentation" width="560" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:16px;overflow:hidden;border:1px solid #e4e4e7;">
          <tr><td style="background:#1e3a8a;padding:28px 32px;text-align:center;">
            <h1 style="margin:0;color:#ffffff;font-size:22px;letter-spacing:.02em;">Artesanías Perú</h1>
          </td></tr>
          <tr><td style="padding:32px;">
            <h2 style="margin:0 0 12px;font-size:24px;color:#0f172a;">¡Pago confirmado!</h2>
            <p style="margin:0 0 16px;line-height:1.55;">Hola <strong>{cliente}</strong>, recibimos el pago de tu pedido correctamente.</p>
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:20px 0;">
              <tr>
                <td style="padding:10px 0;border-bottom:1px solid #e4e4e7;color:#71717a;font-size:14px;">Número de pedido</td>
                <td style="padding:10px 0;border-bottom:1px solid #e4e4e7;color:#0f172a;font-weight:600;text-align:right;">{numeroPedido}</td>
              </tr>
              <tr>
                <td style="padding:10px 0;color:#71717a;font-size:14px;">Total pagado</td>
                <td style="padding:10px 0;color:#1e3a8a;font-weight:700;text-align:right;">S/ {totalPedido}</td>
              </tr>
            </table>
            <div style="margin:20px 0;padding:14px 18px;background:#eff6ff;border-left:4px solid #1e3a8a;border-radius:8px;">
              <p style="margin:0;line-height:1.55;color:#1e3a8a;">Los artesanos ya pueden prepararlo. Te avisaremos cuando tu pedido sea enviado.</p>
            </div>
            <p style="margin:0 0 8px;line-height:1.55;">Puedes ver el detalle en <strong>Mis pedidos</strong>.</p>
            <p style="margin:24px 0 0;font-size:13px;color:#71717a;">Gracias por comprar en Artesanías Perú.</p>
          </td></tr>
          <tr><td style="background:#f4f4f5;padding:16px 32px;text-align:center;font-size:12px;color:#71717a;">
            © Artesanías Perú — Hecho a mano, con historia
          </td></tr>
        </table>
      </td></tr>
    </table>
  </body>
</html>$html$,

    asunto_pedido_enviado_cliente = 'Tu pedido {numeroPedido} está en camino',
    cuerpo_pedido_enviado_cliente = $html$<!DOCTYPE html>
<html lang="es">
  <body style="margin:0;padding:0;background:#f4f4f5;font-family:Segoe UI,Helvetica,Arial,sans-serif;color:#27272a;">
    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="padding:32px 0;">
      <tr><td align="center">
        <table role="presentation" width="560" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:16px;overflow:hidden;border:1px solid #e4e4e7;">
          <tr><td style="background:#1e3a8a;padding:28px 32px;text-align:center;">
            <h1 style="margin:0;color:#ffffff;font-size:22px;letter-spacing:.02em;">Artesanías Perú</h1>
          </td></tr>
          <tr><td style="padding:32px;">
            <h2 style="margin:0 0 12px;font-size:24px;color:#0f172a;">¡Tu pedido está en camino!</h2>
            <p style="margin:0 0 16px;line-height:1.55;">Hola <strong>{cliente}</strong>, el artesano de <strong>{nombreTienda}</strong> ya envió tu pedido.</p>
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:20px 0;">
              <tr>
                <td style="padding:10px 0;border-bottom:1px solid #e4e4e7;color:#71717a;font-size:14px;">Número de pedido</td>
                <td style="padding:10px 0;border-bottom:1px solid #e4e4e7;color:#0f172a;font-weight:600;text-align:right;">{numeroPedido}</td>
              </tr>
              <tr>
                <td style="padding:10px 0;border-bottom:1px solid #e4e4e7;color:#71717a;font-size:14px;">Tienda</td>
                <td style="padding:10px 0;border-bottom:1px solid #e4e4e7;color:#0f172a;font-weight:600;text-align:right;">{nombreTienda}</td>
              </tr>
              <tr>
                <td style="padding:10px 0;border-bottom:1px solid #e4e4e7;color:#71717a;font-size:14px;">Código de seguimiento</td>
                <td style="padding:10px 0;border-bottom:1px solid #e4e4e7;color:#1e3a8a;font-weight:700;text-align:right;font-family:Consolas,Menlo,monospace;">{codigoSeguimiento}</td>
              </tr>
              <tr>
                <td style="padding:10px 0;border-bottom:1px solid #e4e4e7;color:#71717a;font-size:14px;">Código de orden (agencia)</td>
                <td style="padding:10px 0;border-bottom:1px solid #e4e4e7;color:#0f172a;font-weight:600;text-align:right;font-family:Consolas,Menlo,monospace;">{codigoOrdenAgencia}</td>
              </tr>
              <tr>
                <td style="padding:10px 0;color:#71717a;font-size:14px;">Clave de recojo</td>
                <td style="padding:10px 0;color:#1e3a8a;font-weight:700;text-align:right;font-family:Consolas,Menlo,monospace;letter-spacing:.08em;">{claveRecojo}</td>
              </tr>
            </table>
            <div style="margin:16px 0;padding:14px 18px;background:#f4f4f5;border-radius:8px;">
              <p style="margin:0 0 6px;font-size:13px;color:#71717a;">Detalles del envío</p>
              <p style="margin:0;line-height:1.55;color:#27272a;white-space:pre-line;">{detallesEnvio}</p>
            </div>
            <p style="margin:0 0 8px;line-height:1.55;">Guarda estos datos para reclamar tu paquete en la agencia.</p>
            <p style="margin:24px 0 0;font-size:13px;color:#71717a;">Si tienes preguntas, contacta al artesano desde tu panel.</p>
          </td></tr>
          <tr><td style="background:#f4f4f5;padding:16px 32px;text-align:center;font-size:12px;color:#71717a;">
            © Artesanías Perú — Hecho a mano, con historia
          </td></tr>
        </table>
      </td></tr>
    </table>
  </body>
</html>$html$
WHERE correo_id = (SELECT correo_id FROM xqm_configuracion.correo ORDER BY correo_id LIMIT 1);
