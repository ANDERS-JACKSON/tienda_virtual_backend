-- Bloque 1 de remediación de seguridad (C2, A3):
-- - Contador de intentos fallidos + bloqueo temporal para login y para
--   "establecer-contrasena-inicial" (clave temporal de 6 dígitos).
-- - Vencimiento de la clave temporal (24 horas) para que un código viejo
--   filtrado no sirva para siempre.
-- - Contador de intentos fallidos + bloqueo temporal para la verificación
--   de 2FA (TOTP).
-- - Último "paso" TOTP aceptado, para rechazar un código reutilizado
--   (replay) dentro de la misma ventana de validez.

ALTER TABLE xqm_seguridad.usuario
    ADD COLUMN IF NOT EXISTS intentos_fallidos_acceso smallint NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS bloqueado_hasta timestamp with time zone NULL,
    ADD COLUMN IF NOT EXISTS clave_temporal_expira timestamp with time zone NULL,
    ADD COLUMN IF NOT EXISTS intentos_2fa smallint NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS bloqueo_2fa_hasta timestamp with time zone NULL,
    ADD COLUMN IF NOT EXISTS totp_ultimo_paso bigint NULL;

-- Las cuentas que ya tienen clave temporal pendiente (ForzarCambioClave=true)
-- y no tenían vencimiento, reciben 24h desde ahora para no dejarlas
-- bloqueadas de golpe ni abrir una ventana infinita.
UPDATE xqm_seguridad.usuario
SET clave_temporal_expira = now() + interval '24 hours'
WHERE forzar_cambio_clave = true
  AND clave_temporal_expira IS NULL;
