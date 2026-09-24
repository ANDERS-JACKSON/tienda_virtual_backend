-- Fecha de primera publicación (UTC) — base del módulo Novedades (< 30 días).
-- Idempotente: seguro re-ejecutar.

ALTER TABLE xqm_catalogo.producto
    ADD COLUMN IF NOT EXISTS fecha_publicacion timestamptz NULL;

UPDATE xqm_catalogo.producto
SET fecha_publicacion = NOW() AT TIME ZONE 'utc'
WHERE fecha_publicacion IS NULL
  AND estado = 2; -- TipoEstadoProducto.Activo

CREATE INDEX IF NOT EXISTS idx_producto_fecha_publicacion
    ON xqm_catalogo.producto (fecha_publicacion DESC NULLS LAST);

COMMENT ON COLUMN xqm_catalogo.producto.fecha_publicacion IS
    'UTC. Primera vez que el producto pasó a Activo. Usado por Novedades.';
