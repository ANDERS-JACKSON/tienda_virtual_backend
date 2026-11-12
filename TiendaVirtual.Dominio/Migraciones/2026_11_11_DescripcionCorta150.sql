-- Descripción corta del producto/patrón: de 300 a 150 caracteres.
-- Recorta valores existentes (data de prueba) y achica la columna.
-- Ejecutar una sola vez contra tienda_virtual_db.

ALTER TABLE xqm_catalogo.producto
    ALTER COLUMN descripcion_corta TYPE varchar(150)
    USING CASE
        WHEN descripcion_corta IS NULL THEN NULL
        ELSE left(descripcion_corta, 150)
    END;
