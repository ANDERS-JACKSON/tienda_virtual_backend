-- ============================================================================
--  Compra como invitado, aceptación de términos y código de acceso por correo
--  Fecha: 2026-10-22
--
--  Ejecutar en PostgreSQL contra la BD de la tienda (idempotente).
-- ============================================================================

ALTER TABLE xqm_seguridad.usuario
    ADD COLUMN IF NOT EXISTS es_invitado boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS fecha_aceptacion_terminos timestamp with time zone NULL;

CREATE TABLE IF NOT EXISTS xqm_seguridad.codigo_acceso_correo (
    codigo_acceso_id uuid PRIMARY KEY,
    correo citext NOT NULL,
    codigo_hash varchar(128) NOT NULL,
    expira_en timestamp with time zone NOT NULL,
    intentos smallint NOT NULL DEFAULT 0,
    usado boolean NOT NULL DEFAULT false,
    fecha_creacion timestamp with time zone NOT NULL,
    direccion_ip varchar(64) NULL
);

CREATE INDEX IF NOT EXISTS idx_codigo_acceso_correo_lookup
    ON xqm_seguridad.codigo_acceso_correo (correo, fecha_creacion DESC);

CREATE INDEX IF NOT EXISTS idx_codigo_acceso_correo_ip
    ON xqm_seguridad.codigo_acceso_correo (direccion_ip, fecha_creacion DESC);
