-- ============================================================================
--  SEED — xqm_configuracion.correo
--  Configuración SMTP + plantillas de correo (creación de usuario y
--  recuperación de clave). Asume que la tabla YA existe (la define
--  database/schema.sql). Si está vacía, inserta una fila base que el
--  EmailServicio tomará "ORDER BY correo_id DESC LIMIT 1".
--
--  IMPORTANTE — Antes de usarlo en PROD:
--    1) Reemplaza el correo_electronico y contrasenia por los reales.
--    2) Si usas Gmail, genera una "Contraseña de aplicación":
--       https://myaccount.google.com/apppasswords
--    3) Para Gmail (TLS): servidor=smtp.gmail.com, puerto=587
-- ============================================================================

INSERT INTO xqm_configuracion.correo (
    remitente,
    correo_electronico,
    puerto,
    contrasenia,
    servidor_smtp,
    asunto_creacion_usuario,
    cuerpo_creacion_usuario,
    asunto_recuperacion_clave,
    cuerpo_recuperacion_clave,
    asunto_nuevo_pedido_vendedor,
    cuerpo_nuevo_pedido_vendedor,
    asunto_pedido_enviado_cliente,
    cuerpo_pedido_enviado_cliente,
    asunto_verificacion_resultado,
    cuerpo_verificacion_resultado
) VALUES (
    'Artesanías Perú',
    'no-reply@tu-dominio.com',
    587,
    'TU_PASSWORD_DE_APP_AQUI',
    'smtp.gmail.com',

    -- ─── Creación de usuario ────────────────────────────────────────────
    'Bienvenido(a) a Artesanías Perú — tu contraseña de acceso',
    '<!DOCTYPE html>
<html lang="es">
  <body style="margin:0;padding:0;background:#f4f4f5;font-family:Segoe UI,Helvetica,Arial,sans-serif;color:#27272a;">
    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="padding:32px 0;">
      <tr><td align="center">
        <table role="presentation" width="560" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:16px;overflow:hidden;border:1px solid #e4e4e7;">
          <tr><td style="background:#1e3a8a;padding:28px 32px;text-align:center;">
            <h1 style="margin:0;color:#ffffff;font-size:22px;letter-spacing:.02em;">Artesanías Perú</h1>
          </td></tr>
          <tr><td style="padding:32px;">
            <h2 style="margin:0 0 12px;font-size:24px;color:#0f172a;">¡Gracias por registrarte!</h2>
            <p style="margin:0 0 16px;line-height:1.55;">Hola <strong>{usuario}</strong>, hemos creado tu cuenta correctamente.</p>
            <p style="margin:0 0 8px;line-height:1.55;">Usa la siguiente contraseña temporal para acceder a la tienda:</p>
            <div style="margin:20px 0;padding:18px 24px;background:#f4f4f5;border:1px dashed #a1a1aa;border-radius:12px;text-align:center;">
              <p style="margin:0;font-family:Consolas,Menlo,monospace;font-size:22px;font-weight:700;letter-spacing:.18em;color:#1e3a8a;">{clave}</p>
            </div>
            <p style="margin:0 0 8px;line-height:1.55;">Por seguridad, te recomendamos cambiarla una vez que ingreses.</p>
            <p style="margin:24px 0 0;font-size:13px;color:#71717a;">Si no fuiste tú quien creó esta cuenta, ignora este mensaje.</p>
          </td></tr>
          <tr><td style="background:#f4f4f5;padding:16px 32px;text-align:center;font-size:12px;color:#71717a;">
            © Artesanías Perú — Hecho a mano, con historia
          </td></tr>
        </table>
      </td></tr>
    </table>
  </body>
</html>',

    -- ─── Recuperación de clave ──────────────────────────────────────────
    'Tu nueva contraseña de Artesanías Perú',
    '<!DOCTYPE html>
<html lang="es">
  <body style="margin:0;padding:0;background:#f4f4f5;font-family:Segoe UI,Helvetica,Arial,sans-serif;color:#27272a;">
    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="padding:32px 0;">
      <tr><td align="center">
        <table role="presentation" width="560" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:16px;overflow:hidden;border:1px solid #e4e4e7;">
          <tr><td style="background:#1e3a8a;padding:28px 32px;text-align:center;">
            <h1 style="margin:0;color:#ffffff;font-size:22px;letter-spacing:.02em;">Artesanías Perú</h1>
          </td></tr>
          <tr><td style="padding:32px;">
            <h2 style="margin:0 0 12px;font-size:24px;color:#0f172a;">Restablecimos tu contraseña</h2>
            <p style="margin:0 0 16px;line-height:1.55;">Hola <strong>{usuario}</strong>, generamos una nueva contraseña temporal para tu cuenta.</p>
            <div style="margin:20px 0;padding:18px 24px;background:#f4f4f5;border:1px dashed #a1a1aa;border-radius:12px;text-align:center;">
              <p style="margin:0;font-family:Consolas,Menlo,monospace;font-size:22px;font-weight:700;letter-spacing:.18em;color:#1e3a8a;">{clave}</p>
            </div>
            <p style="margin:0 0 8px;line-height:1.55;">Inicia sesión y cámbiala lo antes posible desde tu perfil.</p>
            <p style="margin:24px 0 0;font-size:13px;color:#71717a;">Si no solicitaste este cambio, contáctanos de inmediato.</p>
          </td></tr>
          <tr><td style="background:#f4f4f5;padding:16px 32px;text-align:center;font-size:12px;color:#71717a;">
            © Artesanías Perú — Hecho a mano, con historia
          </td></tr>
        </table>
      </td></tr>
    </table>
  </body>
</html>',

    -- ─── Resto de plantillas (placeholder para futuro) ──────────────────
    NULL, NULL, NULL, NULL, NULL, NULL
);
