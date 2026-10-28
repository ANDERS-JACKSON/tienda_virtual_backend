-- Bloque 4/5: auditoría de eventos de seguridad + versión de consentimiento
-- (términos + privacidad). Ejecutar a mano, igual que el resto de migraciones.

ALTER TABLE xqm_seguridad.usuario
    ADD COLUMN IF NOT EXISTS version_politicas varchar(32) NULL;

CREATE TABLE IF NOT EXISTS xqm_seguridad.evento_auditoria (
    evento_id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    fecha timestamptz NOT NULL DEFAULT now(),
    accion varchar(80) NOT NULL,
    usuario_id uuid NULL,
    direccion_ip varchar(45) NULL,
    detalle varchar(500) NULL
);

CREATE INDEX IF NOT EXISTS idx_evento_auditoria_fecha
    ON xqm_seguridad.evento_auditoria (fecha DESC);

CREATE INDEX IF NOT EXISTS idx_evento_auditoria_usuario
    ON xqm_seguridad.evento_auditoria (usuario_id);
