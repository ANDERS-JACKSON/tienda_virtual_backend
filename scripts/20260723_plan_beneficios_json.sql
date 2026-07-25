-- Beneficios dinámicos de planes (JSON en la misma tabla; sin tabla nueva).
-- Idempotente. Copia de tienda_virtual_oficial/database/migrations/20260723_plan_beneficios_json.sql

ALTER TABLE xqm_vendedor.plan
    ADD COLUMN IF NOT EXISTS beneficios jsonb NULL;

COMMENT ON COLUMN xqm_vendedor.plan.beneficios IS
    'Marketing: etiqueta, heredaDePlanId, destacado, notaPie, items[{texto,incluido}]';

UPDATE xqm_vendedor.plan
SET beneficios = '{
  "etiqueta": "Incluye",
  "heredaDePlanId": null,
  "destacado": false,
  "notaPie": null,
  "items": [
    {"texto": "Tienda pública en el marketplace", "incluido": true},
    {"texto": "Panel de pedidos y productos", "incluido": true},
    {"texto": "Sin soporte por correo", "incluido": false}
  ]
}'::jsonb
WHERE codigo = 'INICIAL'
  AND (beneficios IS NULL OR beneficios = 'null'::jsonb);

UPDATE xqm_vendedor.plan p
SET beneficios = jsonb_build_object(
  'etiqueta', NULL,
  'heredaDePlanId', (SELECT plan_id FROM xqm_vendedor.plan WHERE codigo = 'INICIAL' LIMIT 1),
  'destacado', true,
  'notaPie', 'Ideal para crecer con acompañamiento',
  'items', jsonb_build_array(
    jsonb_build_object('texto', 'Soporte por correo (respuesta en 48 h)', 'incluido', true),
    jsonb_build_object('texto', 'Estadísticas básicas de ventas', 'incluido', true),
    jsonb_build_object('texto', 'Perfil de tienda personalizable', 'incluido', true)
  )
)
WHERE p.codigo = 'ARTESANO'
  AND (p.beneficios IS NULL OR p.beneficios = 'null'::jsonb);

UPDATE xqm_vendedor.plan p
SET beneficios = jsonb_build_object(
  'etiqueta', NULL,
  'heredaDePlanId', (SELECT plan_id FROM xqm_vendedor.plan WHERE codigo = 'ARTESANO' LIMIT 1),
  'destacado', false,
  'notaPie', 'Máxima visibilidad y herramientas pro',
  'items', jsonb_build_array(
    jsonb_build_object('texto', 'Soporte prioritario (respuesta en 24 h)', 'incluido', true),
    jsonb_build_object('texto', 'Destacado en página principal del marketplace', 'incluido', true),
    jsonb_build_object('texto', 'Estadísticas avanzadas y reportes exportables', 'incluido', true),
    jsonb_build_object('texto', 'Cupones y promociones para tu tienda', 'incluido', true),
    jsonb_build_object('texto', 'Acceso anticipado a nuevas funciones', 'incluido', true)
  )
)
WHERE p.codigo = 'MAESTRO'
  AND (p.beneficios IS NULL OR p.beneficios = 'null'::jsonb);

UPDATE xqm_vendedor.plan
SET beneficios = '{
  "etiqueta": "Incluye",
  "heredaDePlanId": null,
  "destacado": false,
  "notaPie": null,
  "items": [
    {"texto": "Tienda pública en el marketplace", "incluido": true},
    {"texto": "Panel de pedidos y productos", "incluido": true}
  ]
}'::jsonb
WHERE beneficios IS NULL;
