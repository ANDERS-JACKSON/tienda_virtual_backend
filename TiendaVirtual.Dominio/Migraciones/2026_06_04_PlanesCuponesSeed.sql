-- Seed planes y cupones de lanzamiento (idempotente por código)
INSERT INTO xqm_vendedor.plan (codigo, nombre, descripcion, precio, periodo, max_productos, tasa_comision, activo)
SELECT 'INICIAL', 'Inicial', 'Perfecto para empezar a vender. Hasta 20 productos.', 19.90, 1, 20, 12.00, true
WHERE NOT EXISTS (SELECT 1 FROM xqm_vendedor.plan WHERE codigo = 'INICIAL');

INSERT INTO xqm_vendedor.plan (codigo, nombre, descripcion, precio, periodo, max_productos, tasa_comision, activo)
SELECT 'ARTESANO', 'Artesano', 'Para vendedores con catálogo amplio. Hasta 100 productos.', 49.90, 1, 100, 10.00, true
WHERE NOT EXISTS (SELECT 1 FROM xqm_vendedor.plan WHERE codigo = 'ARTESANO');

INSERT INTO xqm_vendedor.plan (codigo, nombre, descripcion, precio, periodo, max_productos, tasa_comision, activo)
SELECT 'MAESTRO', 'Maestro', 'Sin límite de productos. La comisión más baja.', 99.90, 1, NULL, 8.00, true
WHERE NOT EXISTS (SELECT 1 FROM xqm_vendedor.plan WHERE codigo = 'MAESTRO');

INSERT INTO xqm_vendedor.cupon (codigo, tipo_descuento, valor_descuento, meses_gratis, usos_maximos, valido_hasta, activo)
SELECT 'LANZAMIENTO2025', 3, NULL, 3, 500, '2026-12-31 23:59:59+00', true
WHERE NOT EXISTS (SELECT 1 FROM xqm_vendedor.cupon WHERE codigo = 'LANZAMIENTO2025');

INSERT INTO xqm_vendedor.cupon (codigo, tipo_descuento, valor_descuento, meses_gratis, usos_maximos, valido_hasta, activo)
SELECT 'BIENVENIDA50', 1, 50.00, 0, 1000, '2026-12-31 23:59:59+00', true
WHERE NOT EXISTS (SELECT 1 FROM xqm_vendedor.cupon WHERE codigo = 'BIENVENIDA50');

INSERT INTO xqm_vendedor.cupon (codigo, tipo_descuento, valor_descuento, meses_gratis, usos_maximos, valido_hasta, activo)
SELECT 'ARTESANO20', 2, 20.00, 0, NULL, '2026-12-31 23:59:59+00', true
WHERE NOT EXISTS (SELECT 1 FROM xqm_vendedor.cupon WHERE codigo = 'ARTESANO20');

INSERT INTO xqm_vendedor.cupon (codigo, tipo_descuento, valor_descuento, meses_gratis, usos_maximos, valido_hasta, activo)
SELECT 'AMIGO2X1', 3, NULL, 1, NULL, NULL, true
WHERE NOT EXISTS (SELECT 1 FROM xqm_vendedor.cupon WHERE codigo = 'AMIGO2X1');
