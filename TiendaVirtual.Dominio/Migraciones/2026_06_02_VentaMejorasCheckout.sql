-- =====================================================================
-- Migración: Mejoras del módulo de venta para soportar el checkout
-- Fecha: 2026-06-02
--
-- Aplica 7 cambios sobre el schema xqm_venta:
--   1. Crear tabla metodo_envio + seed
--   2. Agregar metodo_envio_id a suborden
--   3. Índice único en carrito(usuario_id)
--   4. Índice único en item_carrito(carrito_id, variante_id)
--   5. fecha_actualizacion en carrito
--   6. fecha_agregado en item_carrito
--   7. Ampliar estados de orden (1..7) en check
--
-- Diseño defensivo: usa IF NOT EXISTS / DO blocks para que se pueda re-correr.
-- =====================================================================

BEGIN;

-- ------------------------------------------------------------------
-- 1. Tabla de métodos de envío
-- ------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS xqm_venta.metodo_envio (
    metodo_envio_id        serial PRIMARY KEY,
    codigo                 varchar(20)  NOT NULL,
    nombre                 varchar(100) NOT NULL,
    descripcion            varchar(500),
    monto_base             numeric(10,2) NOT NULL DEFAULT 0,
    tiempo_estimado_dias   integer       NOT NULL DEFAULT 3,
    activo                 boolean       NOT NULL DEFAULT true,
    orden                  integer       NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_metodo_envio_codigo
    ON xqm_venta.metodo_envio (codigo);

-- Seed inicial (idempotente).
INSERT INTO xqm_venta.metodo_envio (codigo, nombre, descripcion, monto_base, tiempo_estimado_dias, orden)
VALUES
    ('RECOJO', 'Recojo en tienda', 'El comprador recoge directamente del artesano.',  0.00, 0, 1),
    ('SHALOM', 'Shalom',           'Envío nacional a agencia Shalom.',                15.00, 4, 2),
    ('OLVA',   'Olva Courier',     'Envío a domicilio en Lima Metropolitana.',        20.00, 2, 3)
ON CONFLICT (codigo) DO NOTHING;

-- ------------------------------------------------------------------
-- 2. metodo_envio_id en suborden
-- ------------------------------------------------------------------
ALTER TABLE xqm_venta.suborden
    ADD COLUMN IF NOT EXISTS metodo_envio_id integer NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.table_constraints
        WHERE table_schema = 'xqm_venta'
          AND table_name   = 'suborden'
          AND constraint_name = 'fk_suborden_metodo_envio'
    ) THEN
        ALTER TABLE xqm_venta.suborden
            ADD CONSTRAINT fk_suborden_metodo_envio
            FOREIGN KEY (metodo_envio_id)
            REFERENCES xqm_venta.metodo_envio (metodo_envio_id)
            ON DELETE RESTRICT;
    END IF;
END $$;

-- ------------------------------------------------------------------
-- 3. Índice único en carrito(usuario_id) — 1 carrito por usuario
-- ------------------------------------------------------------------
CREATE UNIQUE INDEX IF NOT EXISTS uq_carrito_usuario
    ON xqm_venta.carrito (usuario_id);

-- ------------------------------------------------------------------
-- 4. Índice único en item_carrito(carrito_id, variante_id)
--    Evita filas duplicadas; el código incrementa la cantidad en su lugar.
-- ------------------------------------------------------------------
CREATE UNIQUE INDEX IF NOT EXISTS uq_item_carrito_variante
    ON xqm_venta.item_carrito (carrito_id, variante_id);

-- ------------------------------------------------------------------
-- 5. fecha_actualizacion en carrito
-- ------------------------------------------------------------------
ALTER TABLE xqm_venta.carrito
    ADD COLUMN IF NOT EXISTS fecha_actualizacion timestamp NOT NULL DEFAULT now();

-- ------------------------------------------------------------------
-- 6. fecha_agregado en item_carrito
-- ------------------------------------------------------------------
ALTER TABLE xqm_venta.item_carrito
    ADD COLUMN IF NOT EXISTS fecha_agregado timestamp NOT NULL DEFAULT now();

-- ------------------------------------------------------------------
-- 7. Estados de orden 1..7 (PendientePago, Pagada, EnPreparacion,
--    EnCamino, Entregada, Cancelada, Disputada).
-- ------------------------------------------------------------------
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.table_constraints
        WHERE table_schema = 'xqm_venta'
          AND table_name   = 'orden'
          AND constraint_name = 'ck_orden_estado'
    ) THEN
        ALTER TABLE xqm_venta.orden DROP CONSTRAINT ck_orden_estado;
    END IF;

    ALTER TABLE xqm_venta.orden
        ADD CONSTRAINT ck_orden_estado
        CHECK (estado BETWEEN 1 AND 7);
END $$;

COMMIT;
