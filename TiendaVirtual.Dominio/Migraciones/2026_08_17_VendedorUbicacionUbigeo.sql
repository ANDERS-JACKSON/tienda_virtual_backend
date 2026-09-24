-- Ubicación pública de la tienda (departamento / provincia / distrito vía ubigeo INEI).
-- Idempotente: seguro re-ejecutar.
-- Requiere tablas xqm_seguridad.departamento|provincia|distrito.

ALTER TABLE xqm_vendedor.vendedor
    ADD COLUMN IF NOT EXISTS distrito_id varchar(6) NULL;

ALTER TABLE xqm_vendedor.vendedor
    ADD COLUMN IF NOT EXISTS ubicacion_departamento varchar(100) NULL;

ALTER TABLE xqm_vendedor.vendedor
    ADD COLUMN IF NOT EXISTS ubicacion_provincia varchar(100) NULL;

ALTER TABLE xqm_vendedor.vendedor
    ADD COLUMN IF NOT EXISTS ubicacion_distrito varchar(100) NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'fk_vendedor_distrito'
    ) THEN
        ALTER TABLE xqm_vendedor.vendedor
            ADD CONSTRAINT fk_vendedor_distrito
            FOREIGN KEY (distrito_id)
            REFERENCES xqm_seguridad.distrito (distrito_id)
            ON UPDATE CASCADE ON DELETE RESTRICT;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_vendedor_distrito
    ON xqm_vendedor.vendedor (distrito_id);

COMMENT ON COLUMN xqm_vendedor.vendedor.distrito_id IS
    'Código ubigeo INEI del distrito (6 dígitos). Ubicación pública de la tienda.';
COMMENT ON COLUMN xqm_vendedor.vendedor.ubicacion_departamento IS
    'Nombre departamento denormalizado al guardar (lectura pública).';
COMMENT ON COLUMN xqm_vendedor.vendedor.ubicacion_provincia IS
    'Nombre provincia denormalizado al guardar (lectura pública).';
COMMENT ON COLUMN xqm_vendedor.vendedor.ubicacion_distrito IS
    'Nombre distrito denormalizado al guardar (lectura pública).';
