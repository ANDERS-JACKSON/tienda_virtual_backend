-- ═════════════════════════════════════════════════════════════════════════════
-- Normalizar ofertas existentes: rellenar `porcentaje_descuento` cuando solo
-- se guardó `precio_oferta`. La regla nueva del sistema calcula precios usando
-- el porcentaje sobre el precio ACTUAL de cada variante, así que cada oferta
-- debe tener el porcentaje definido.
--
-- Referencia: se toma el precio de la variante más barata del producto; si el
-- producto no tiene variantes activas, se cae al `precio_base`. Si `precio_oferta`
-- ya es mayor o igual al precio de referencia, la oferta se desactiva.
-- ═════════════════════════════════════════════════════════════════════════════
BEGIN;

WITH precio_referencia AS (
    SELECT
        p.producto_id,
        COALESCE(
            (
                SELECT MIN(v.precio)
                FROM xqm_catalogo.variante_producto v
                WHERE v.producto_id = p.producto_id AND v.activa = TRUE
            ),
            p.precio_base,
            0
        )::numeric(10,2) AS precio_ref
    FROM xqm_catalogo.producto p
)
UPDATE xqm_catalogo.oferta o
SET porcentaje_descuento = ROUND(
        (1 - o.precio_oferta / pr.precio_ref) * 100,
        2
    )
FROM precio_referencia pr
WHERE o.producto_id = pr.producto_id
  AND o.porcentaje_descuento IS NULL
  AND o.precio_oferta IS NOT NULL
  AND o.precio_oferta > 0
  AND pr.precio_ref > 0
  AND o.precio_oferta < pr.precio_ref;

-- Ofertas cuyo precio fijo iguala o supera al precio de referencia: se marcan
-- como inactivas (ya no aportan descuento real y quedaron inconsistentes).
UPDATE xqm_catalogo.oferta o
SET activa = FALSE
FROM (
    SELECT
        p.producto_id,
        COALESCE(
            (
                SELECT MIN(v.precio)
                FROM xqm_catalogo.variante_producto v
                WHERE v.producto_id = p.producto_id AND v.activa = TRUE
            ),
            p.precio_base,
            0
        )::numeric(10,2) AS precio_ref
    FROM xqm_catalogo.producto p
) pr
WHERE o.producto_id = pr.producto_id
  AND o.porcentaje_descuento IS NULL
  AND o.precio_oferta IS NOT NULL
  AND (pr.precio_ref <= 0 OR o.precio_oferta >= pr.precio_ref);

COMMIT;

-- ─── Verificación ────────────────────────────────────────────────────────────
-- SELECT o.oferta_id, o.producto_id, p.nombre, o.porcentaje_descuento,
--        o.precio_oferta, o.activa, o.fecha_inicio, o.fecha_fin
-- FROM xqm_catalogo.oferta o
-- JOIN xqm_catalogo.producto p ON p.producto_id = o.producto_id
-- ORDER BY o.oferta_id DESC;
