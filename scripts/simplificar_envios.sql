-- ═════════════════════════════════════════════════════════════════════════════
-- Migración: Simplificación del sistema de envíos
--
-- Cambios de negocio:
--   1. Los envíos se hacen por defecto con SHALOM (u otra agencia). El costo
--      del envío se paga directamente en la agencia al recoger; NO se cobra
--      en la orden. Por eso el catálogo de métodos queda con SHALOM activo
--      y los demás se desactivan.
--   2. Para envíos por agencia, se requiere el DNI del receptor. Se agrega
--      la columna `dni_receptor` a `xqm_seguridad.direccion`.
--
-- Este script es idempotente: se puede ejecutar múltiples veces sin efectos
-- adversos.
-- ═════════════════════════════════════════════════════════════════════════════
BEGIN;

-- 1) Agregar columna dni_receptor si no existe
ALTER TABLE xqm_seguridad.direccion
    ADD COLUMN IF NOT EXISTS dni_receptor VARCHAR(20);

COMMENT ON COLUMN xqm_seguridad.direccion.dni_receptor IS
    'DNI (u otro documento) del receptor. Requerido para envíos por agencia.';

-- 1.b) Precio original del item (antes del descuento) para trazabilidad
--      histórica: permite mostrar el precio tachado en el detalle del pedido
--      del comprador y del vendedor incluso si la oferta ya venció o cambió.
ALTER TABLE xqm_venta.item_orden
    ADD COLUMN IF NOT EXISTS precio_original NUMERIC(10, 2);

COMMENT ON COLUMN xqm_venta.item_orden.precio_original IS
    'Precio unitario ANTES del descuento. NULL cuando el item se cobró a precio pleno.';

-- 2) Normalizar catálogo de métodos de envío:
--    - Dejar SHALOM activo con monto_base = 0 (el comprador paga en agencia).
--    - Desactivar RECOJO y OLVA (ya no se ofrecen en el checkout).
--    NOTA: no se eliminan filas para preservar la integridad referencial
--    con órdenes históricas (xqm_venta.suborden.metodo_envio_id).
UPDATE xqm_venta.metodo_envio
SET activo = TRUE,
    monto_base = 0,
    orden = 1
WHERE codigo = 'SHALOM';

UPDATE xqm_venta.metodo_envio
SET activo = FALSE
WHERE codigo IN ('RECOJO', 'OLVA');

-- 3) Órdenes existentes: opcional. Si se desea que los pedidos históricos
--    reflejen el nuevo modelo (envío no incluido en el total), descomentar:
--
-- UPDATE xqm_venta.suborden SET monto_envio = 0;
-- UPDATE xqm_venta.orden o
-- SET total_envio = 0,
--     total = o.subtotal - o.total_descuento
-- WHERE EXISTS (SELECT 1 FROM xqm_venta.suborden s WHERE s.orden_id = o.orden_id);

-- 4) Backfill de `item_orden.precio_original` para pedidos históricos.
--    Estrategia: si el precio actual de la variante (variante_producto.precio)
--    es MAYOR que el precio cobrado (item_orden.precio_unitario), asumimos que
--    hubo un descuento cuando se compró y usamos el precio actual como el
--    "original". Si son iguales o el actual es menor (cambio de precio a la
--    baja), lo dejamos en NULL para no inventar un descuento inexistente.
--
--    IMPORTANTE: esto es una APROXIMACIÓN. No podemos reconstruir con exactitud
--    el precio original real de esos pedidos porque el histórico no lo guardó.
--    Si prefieres no arriesgar datos incorrectos, comenta este bloque.
UPDATE xqm_venta.item_orden AS it
SET precio_original = v.precio
FROM xqm_catalogo.variante_producto AS v
WHERE it.variante_id = v.variante_id
  AND it.precio_original IS NULL
  AND v.precio > it.precio_unitario;

COMMIT;

-- ─── Verificación ────────────────────────────────────────────────────────────
-- SELECT column_name, data_type, character_maximum_length, is_nullable
-- FROM information_schema.columns
-- WHERE table_schema = 'xqm_seguridad' AND table_name = 'direccion'
-- ORDER BY ordinal_position;
--
-- SELECT metodo_envio_id, codigo, nombre, monto_base, activo, orden
-- FROM xqm_venta.metodo_envio
-- ORDER BY orden;
