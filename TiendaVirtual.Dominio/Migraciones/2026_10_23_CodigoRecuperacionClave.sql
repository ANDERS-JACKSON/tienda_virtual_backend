-- Código de recuperación separado del código para entrar.
-- El de acceso sigue en la misma tabla; este propósito no inicia sesión.

ALTER TABLE xqm_seguridad.codigo_acceso_correo
    ADD COLUMN IF NOT EXISTS proposito varchar(32) NOT NULL DEFAULT 'acceso',
    ADD COLUMN IF NOT EXISTS ticket_hash varchar(128) NULL,
    ADD COLUMN IF NOT EXISTS ticket_expira timestamp with time zone NULL;

CREATE INDEX IF NOT EXISTS idx_codigo_acceso_correo_proposito
    ON xqm_seguridad.codigo_acceso_correo (correo, proposito, fecha_creacion DESC);
