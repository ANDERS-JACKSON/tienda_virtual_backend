-- ============================================================
-- Migración: PKs/FKs sensibles de int/bigint → uuid
-- Entorno: NO producción (borra datos de prueba).
-- Requiere: PostgreSQL 13+ con extensión pgcrypto.
-- ============================================================
-- Ejecutar UNA sola vez contra la BD existente.
-- Tras aplicar: reiniciar API y volver a registrar usuarios.
-- ============================================================

CREATE EXTENSION IF NOT EXISTS pgcrypto;

BEGIN;

-- ------------------------------------------------------------
-- 1) Quitar FKs que tocan columnas a migrar
-- ------------------------------------------------------------
ALTER TABLE IF EXISTS xqm_seguridad.usuario DROP CONSTRAINT IF EXISTS fk_usuario_persona;
ALTER TABLE IF EXISTS xqm_seguridad.usuario_rol DROP CONSTRAINT IF EXISTS fk_usuario_rol_usuario;
ALTER TABLE IF EXISTS xqm_seguridad.direccion DROP CONSTRAINT IF EXISTS fk_direccion_persona;
ALTER TABLE IF EXISTS xqm_seguridad.token_refresco DROP CONSTRAINT IF EXISTS fk_token_usuario;
ALTER TABLE IF EXISTS xqm_seguridad.usuario_login_externo DROP CONSTRAINT IF EXISTS fk_usuario_login_externo_usuario;

ALTER TABLE IF EXISTS xqm_vendedor.vendedor DROP CONSTRAINT IF EXISTS fk_vendedor_usuario;
ALTER TABLE IF EXISTS xqm_vendedor.solicitud_verificacion DROP CONSTRAINT IF EXISTS fk_solicitud_vendedor;
ALTER TABLE IF EXISTS xqm_vendedor.solicitud_verificacion DROP CONSTRAINT IF EXISTS fk_solicitud_verificador;
ALTER TABLE IF EXISTS xqm_vendedor.cuenta_bancaria DROP CONSTRAINT IF EXISTS fk_cuenta_vendedor;

ALTER TABLE IF EXISTS xqm_venta.carrito DROP CONSTRAINT IF EXISTS fk_carrito_usuario;
ALTER TABLE IF EXISTS xqm_venta.orden DROP CONSTRAINT IF EXISTS fk_orden_cliente;
ALTER TABLE IF EXISTS xqm_venta.suborden DROP CONSTRAINT IF EXISTS fk_suborden_orden;
ALTER TABLE IF EXISTS xqm_venta.suborden DROP CONSTRAINT IF EXISTS fk_suborden_vendedor;
ALTER TABLE IF EXISTS xqm_venta.item_orden DROP CONSTRAINT IF EXISTS fk_item_orden_suborden;
ALTER TABLE IF EXISTS xqm_venta.envio DROP CONSTRAINT IF EXISTS fk_envio_suborden;

ALTER TABLE IF EXISTS xqm_pago.transaccion DROP CONSTRAINT IF EXISTS fk_transaccion_orden;
ALTER TABLE IF EXISTS xqm_pago.transaccion DROP CONSTRAINT IF EXISTS fk_transaccion_usuario;
ALTER TABLE IF EXISTS xqm_pago.retiro DROP CONSTRAINT IF EXISTS fk_retiro_vendedor;
ALTER TABLE IF EXISTS xqm_pago.retiro DROP CONSTRAINT IF EXISTS fk_retiro_cuenta;
ALTER TABLE IF EXISTS xqm_pago.retiro DROP CONSTRAINT IF EXISTS fk_retiro_procesado;
ALTER TABLE IF EXISTS xqm_pago.movimiento_billetera DROP CONSTRAINT IF EXISTS fk_movimiento_vendedor;

ALTER TABLE IF EXISTS xqm_soporte.resena_producto DROP CONSTRAINT IF EXISTS fk_resena_item_orden;
ALTER TABLE IF EXISTS xqm_soporte.resena_producto DROP CONSTRAINT IF EXISTS fk_resena_cliente;
ALTER TABLE IF EXISTS xqm_soporte.resena_vendedor DROP CONSTRAINT IF EXISTS fk_resena_vendedor_suborden;
ALTER TABLE IF EXISTS xqm_soporte.resena_vendedor DROP CONSTRAINT IF EXISTS fk_resena_vendedor_cliente;
ALTER TABLE IF EXISTS xqm_soporte.reclamo DROP CONSTRAINT IF EXISTS fk_reclamo_suborden;
ALTER TABLE IF EXISTS xqm_soporte.reclamo DROP CONSTRAINT IF EXISTS fk_reclamo_abierto;
ALTER TABLE IF EXISTS xqm_soporte.reclamo DROP CONSTRAINT IF EXISTS fk_reclamo_resuelto;
ALTER TABLE IF EXISTS xqm_soporte.mensaje_reclamo DROP CONSTRAINT IF EXISTS fk_mensaje_reclamo;
ALTER TABLE IF EXISTS xqm_soporte.mensaje_reclamo DROP CONSTRAINT IF EXISTS fk_mensaje_remitente;
ALTER TABLE IF EXISTS xqm_soporte.notificacion DROP CONSTRAINT IF EXISTS fk_notificacion_usuario;
ALTER TABLE IF EXISTS xqm_soporte.mensaje_contacto DROP CONSTRAINT IF EXISTS fk_mensaje_contacto_usuario;
ALTER TABLE IF EXISTS xqm_soporte.mensaje_contacto DROP CONSTRAINT IF EXISTS fk_mensaje_contacto_respondido;

ALTER TABLE IF EXISTS xqm_catalogo.favorito DROP CONSTRAINT IF EXISTS fk_favorito_usuario;

-- ------------------------------------------------------------
-- 2) Vaciar tablas dependientes (datos de prueba)
-- ------------------------------------------------------------
TRUNCATE TABLE
    xqm_soporte.mensaje_reclamo,
    xqm_soporte.reclamo,
    xqm_soporte.resena_producto,
    xqm_soporte.resena_vendedor,
    xqm_soporte.notificacion,
    xqm_pago.retiro,
    xqm_pago.movimiento_billetera,
    xqm_pago.transaccion,
    xqm_venta.envio,
    xqm_venta.item_orden,
    xqm_venta.suborden,
    xqm_venta.orden,
    xqm_venta.item_carrito,
    xqm_venta.carrito,
    xqm_catalogo.favorito,
    xqm_vendedor.solicitud_verificacion,
    xqm_vendedor.cuenta_bancaria,
    xqm_seguridad.token_refresco,
    xqm_seguridad.usuario_login_externo,
    xqm_seguridad.usuario_rol,
    xqm_seguridad.direccion
RESTART IDENTITY CASCADE;

-- mensaje_contacto puede no existir en dumps viejos
DO $$
BEGIN
    IF to_regclass('xqm_soporte.mensaje_contacto') IS NOT NULL THEN
        EXECUTE 'TRUNCATE TABLE xqm_soporte.mensaje_contacto RESTART IDENTITY CASCADE';
    END IF;
END $$;

-- Usuarios / personas / vendedores: vaciar también (JWT y FKs quedarán inválidos)
TRUNCATE TABLE
    xqm_vendedor.vendedor,
    xqm_seguridad.usuario,
    xqm_seguridad.persona
RESTART IDENTITY CASCADE;

-- ------------------------------------------------------------
-- 3) Función auxiliar: convertir columna a uuid (sin identity)
-- ------------------------------------------------------------
CREATE OR REPLACE FUNCTION pg_temp.col_a_uuid(p_schema text, p_table text, p_column text, p_nullable boolean DEFAULT false)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    EXECUTE format(
        'ALTER TABLE %I.%I ALTER COLUMN %I DROP IDENTITY IF EXISTS',
        p_schema, p_table, p_column
    );
    EXECUTE format(
        'ALTER TABLE %I.%I ALTER COLUMN %I TYPE uuid USING gen_random_uuid()',
        p_schema, p_table, p_column
    );
    IF p_nullable THEN
        EXECUTE format(
            'ALTER TABLE %I.%I ALTER COLUMN %I DROP NOT NULL',
            p_schema, p_table, p_column
        );
        EXECUTE format(
            'ALTER TABLE %I.%I ALTER COLUMN %I DROP DEFAULT',
            p_schema, p_table, p_column
        );
    ELSE
        EXECUTE format(
            'ALTER TABLE %I.%I ALTER COLUMN %I SET DEFAULT gen_random_uuid()',
            p_schema, p_table, p_column
        );
        EXECUTE format(
            'ALTER TABLE %I.%I ALTER COLUMN %I SET NOT NULL',
            p_schema, p_table, p_column
        );
    END IF;
END;
$$;

-- ------------------------------------------------------------
-- 4) PKs y FKs → uuid
-- ------------------------------------------------------------
-- persona / usuario / direccion
SELECT pg_temp.col_a_uuid('xqm_seguridad', 'persona', 'persona_id', false);
SELECT pg_temp.col_a_uuid('xqm_seguridad', 'usuario', 'usuario_id', false);
SELECT pg_temp.col_a_uuid('xqm_seguridad', 'usuario', 'persona_id', false);
SELECT pg_temp.col_a_uuid('xqm_seguridad', 'direccion', 'direccion_id', false);
SELECT pg_temp.col_a_uuid('xqm_seguridad', 'direccion', 'persona_id', false);
SELECT pg_temp.col_a_uuid('xqm_seguridad', 'usuario_rol', 'usuario_id', false);
SELECT pg_temp.col_a_uuid('xqm_seguridad', 'token_refresco', 'usuario_id', false);

DO $$
BEGIN
    IF to_regclass('xqm_seguridad.usuario_login_externo') IS NOT NULL THEN
        PERFORM pg_temp.col_a_uuid('xqm_seguridad', 'usuario_login_externo', 'usuario_id', false);
    END IF;
END $$;

-- vendedor FKs a usuario + solicitud/cuenta PKs
SELECT pg_temp.col_a_uuid('xqm_vendedor', 'vendedor', 'usuario_id', false);
SELECT pg_temp.col_a_uuid('xqm_vendedor', 'solicitud_verificacion', 'solicitud_id', false);
SELECT pg_temp.col_a_uuid('xqm_vendedor', 'solicitud_verificacion', 'verificador_id', true);
SELECT pg_temp.col_a_uuid('xqm_vendedor', 'cuenta_bancaria', 'cuenta_id', false);

-- venta
SELECT pg_temp.col_a_uuid('xqm_venta', 'carrito', 'usuario_id', false);
SELECT pg_temp.col_a_uuid('xqm_venta', 'orden', 'orden_id', false);
SELECT pg_temp.col_a_uuid('xqm_venta', 'orden', 'cliente_id', false);
SELECT pg_temp.col_a_uuid('xqm_venta', 'suborden', 'suborden_id', false);
SELECT pg_temp.col_a_uuid('xqm_venta', 'suborden', 'orden_id', false);
SELECT pg_temp.col_a_uuid('xqm_venta', 'item_orden', 'item_orden_id', false);
SELECT pg_temp.col_a_uuid('xqm_venta', 'item_orden', 'suborden_id', false);
SELECT pg_temp.col_a_uuid('xqm_venta', 'envio', 'envio_id', false);
SELECT pg_temp.col_a_uuid('xqm_venta', 'envio', 'suborden_id', false);

-- pago
SELECT pg_temp.col_a_uuid('xqm_pago', 'transaccion', 'transaccion_id', false);
SELECT pg_temp.col_a_uuid('xqm_pago', 'transaccion', 'orden_id', true);
SELECT pg_temp.col_a_uuid('xqm_pago', 'transaccion', 'usuario_id', false);
SELECT pg_temp.col_a_uuid('xqm_pago', 'movimiento_billetera', 'movimiento_id', false);
SELECT pg_temp.col_a_uuid('xqm_pago', 'movimiento_billetera', 'referencia_id', true);
SELECT pg_temp.col_a_uuid('xqm_pago', 'retiro', 'retiro_id', false);
SELECT pg_temp.col_a_uuid('xqm_pago', 'retiro', 'cuenta_id', false);
SELECT pg_temp.col_a_uuid('xqm_pago', 'retiro', 'procesado_por', true);

-- soporte
SELECT pg_temp.col_a_uuid('xqm_soporte', 'resena_producto', 'item_orden_id', false);
SELECT pg_temp.col_a_uuid('xqm_soporte', 'resena_producto', 'cliente_id', false);
SELECT pg_temp.col_a_uuid('xqm_soporte', 'reclamo', 'reclamo_id', false);
SELECT pg_temp.col_a_uuid('xqm_soporte', 'reclamo', 'suborden_id', false);
SELECT pg_temp.col_a_uuid('xqm_soporte', 'reclamo', 'abierto_por', false);
SELECT pg_temp.col_a_uuid('xqm_soporte', 'reclamo', 'resuelto_por', true);
SELECT pg_temp.col_a_uuid('xqm_soporte', 'mensaje_reclamo', 'mensaje_id', false);
SELECT pg_temp.col_a_uuid('xqm_soporte', 'mensaje_reclamo', 'reclamo_id', false);
SELECT pg_temp.col_a_uuid('xqm_soporte', 'mensaje_reclamo', 'remitente_id', false);
SELECT pg_temp.col_a_uuid('xqm_soporte', 'notificacion', 'notificacion_id', false);
SELECT pg_temp.col_a_uuid('xqm_soporte', 'notificacion', 'usuario_id', false);

DO $$
BEGIN
    IF to_regclass('xqm_soporte.resena_vendedor') IS NOT NULL THEN
        PERFORM pg_temp.col_a_uuid('xqm_soporte', 'resena_vendedor', 'suborden_id', false);
        PERFORM pg_temp.col_a_uuid('xqm_soporte', 'resena_vendedor', 'cliente_id', false);
    END IF;
    IF to_regclass('xqm_soporte.mensaje_contacto') IS NOT NULL THEN
        PERFORM pg_temp.col_a_uuid('xqm_soporte', 'mensaje_contacto', 'mensaje_contacto_id', false);
        PERFORM pg_temp.col_a_uuid('xqm_soporte', 'mensaje_contacto', 'usuario_id', true);
        PERFORM pg_temp.col_a_uuid('xqm_soporte', 'mensaje_contacto', 'respondido_por', true);
    END IF;
END $$;

-- favorito
SELECT pg_temp.col_a_uuid('xqm_catalogo', 'favorito', 'usuario_id', false);

-- ------------------------------------------------------------
-- 5) Recrear FKs
-- ------------------------------------------------------------
ALTER TABLE xqm_seguridad.usuario
    ADD CONSTRAINT fk_usuario_persona FOREIGN KEY (persona_id)
    REFERENCES xqm_seguridad.persona(persona_id) ON DELETE RESTRICT;

ALTER TABLE xqm_seguridad.usuario_rol
    ADD CONSTRAINT fk_usuario_rol_usuario FOREIGN KEY (usuario_id)
    REFERENCES xqm_seguridad.usuario(usuario_id) ON DELETE CASCADE;

ALTER TABLE xqm_seguridad.direccion
    ADD CONSTRAINT fk_direccion_persona FOREIGN KEY (persona_id)
    REFERENCES xqm_seguridad.persona(persona_id) ON DELETE CASCADE;

ALTER TABLE xqm_seguridad.token_refresco
    ADD CONSTRAINT fk_token_usuario FOREIGN KEY (usuario_id)
    REFERENCES xqm_seguridad.usuario(usuario_id) ON DELETE CASCADE;

DO $$
BEGIN
    IF to_regclass('xqm_seguridad.usuario_login_externo') IS NOT NULL THEN
        ALTER TABLE xqm_seguridad.usuario_login_externo
            ADD CONSTRAINT fk_usuario_login_externo_usuario FOREIGN KEY (usuario_id)
            REFERENCES xqm_seguridad.usuario(usuario_id) ON DELETE CASCADE;
    END IF;
END $$;

ALTER TABLE xqm_vendedor.vendedor
    ADD CONSTRAINT fk_vendedor_usuario FOREIGN KEY (usuario_id)
    REFERENCES xqm_seguridad.usuario(usuario_id) ON DELETE RESTRICT;

ALTER TABLE xqm_vendedor.solicitud_verificacion
    ADD CONSTRAINT fk_solicitud_vendedor FOREIGN KEY (vendedor_id)
    REFERENCES xqm_vendedor.vendedor(vendedor_id) ON DELETE CASCADE;

ALTER TABLE xqm_vendedor.solicitud_verificacion
    ADD CONSTRAINT fk_solicitud_verificador FOREIGN KEY (verificador_id)
    REFERENCES xqm_seguridad.usuario(usuario_id) ON DELETE SET NULL;

ALTER TABLE xqm_vendedor.cuenta_bancaria
    ADD CONSTRAINT fk_cuenta_vendedor FOREIGN KEY (vendedor_id)
    REFERENCES xqm_vendedor.vendedor(vendedor_id) ON DELETE CASCADE;

ALTER TABLE xqm_venta.carrito
    ADD CONSTRAINT fk_carrito_usuario FOREIGN KEY (usuario_id)
    REFERENCES xqm_seguridad.usuario(usuario_id) ON DELETE CASCADE;

ALTER TABLE xqm_venta.orden
    ADD CONSTRAINT fk_orden_cliente FOREIGN KEY (cliente_id)
    REFERENCES xqm_seguridad.usuario(usuario_id) ON DELETE RESTRICT;

ALTER TABLE xqm_venta.suborden
    ADD CONSTRAINT fk_suborden_orden FOREIGN KEY (orden_id)
    REFERENCES xqm_venta.orden(orden_id) ON DELETE CASCADE;

ALTER TABLE xqm_venta.item_orden
    ADD CONSTRAINT fk_item_orden_suborden FOREIGN KEY (suborden_id)
    REFERENCES xqm_venta.suborden(suborden_id) ON DELETE CASCADE;

ALTER TABLE xqm_venta.envio
    ADD CONSTRAINT fk_envio_suborden FOREIGN KEY (suborden_id)
    REFERENCES xqm_venta.suborden(suborden_id) ON DELETE CASCADE;

ALTER TABLE xqm_pago.transaccion
    ADD CONSTRAINT fk_transaccion_orden FOREIGN KEY (orden_id)
    REFERENCES xqm_venta.orden(orden_id) ON DELETE SET NULL;

ALTER TABLE xqm_pago.transaccion
    ADD CONSTRAINT fk_transaccion_usuario FOREIGN KEY (usuario_id)
    REFERENCES xqm_seguridad.usuario(usuario_id) ON DELETE RESTRICT;

ALTER TABLE xqm_pago.retiro
    ADD CONSTRAINT fk_retiro_cuenta FOREIGN KEY (cuenta_id)
    REFERENCES xqm_vendedor.cuenta_bancaria(cuenta_id) ON DELETE RESTRICT;

ALTER TABLE xqm_pago.retiro
    ADD CONSTRAINT fk_retiro_procesado FOREIGN KEY (procesado_por)
    REFERENCES xqm_seguridad.usuario(usuario_id) ON DELETE SET NULL;

ALTER TABLE xqm_pago.retiro
    ADD CONSTRAINT fk_retiro_vendedor FOREIGN KEY (vendedor_id)
    REFERENCES xqm_vendedor.vendedor(vendedor_id) ON DELETE RESTRICT;

ALTER TABLE xqm_pago.movimiento_billetera
    ADD CONSTRAINT fk_movimiento_vendedor FOREIGN KEY (vendedor_id)
    REFERENCES xqm_vendedor.vendedor(vendedor_id) ON DELETE CASCADE;

ALTER TABLE xqm_soporte.resena_producto
    ADD CONSTRAINT fk_resena_item_orden FOREIGN KEY (item_orden_id)
    REFERENCES xqm_venta.item_orden(item_orden_id) ON DELETE RESTRICT;

ALTER TABLE xqm_soporte.resena_producto
    ADD CONSTRAINT fk_resena_cliente FOREIGN KEY (cliente_id)
    REFERENCES xqm_seguridad.usuario(usuario_id) ON DELETE RESTRICT;

DO $$
BEGIN
    IF to_regclass('xqm_soporte.resena_vendedor') IS NOT NULL THEN
        ALTER TABLE xqm_soporte.resena_vendedor
            ADD CONSTRAINT fk_resena_vendedor_suborden FOREIGN KEY (suborden_id)
            REFERENCES xqm_venta.suborden(suborden_id) ON DELETE RESTRICT;
        ALTER TABLE xqm_soporte.resena_vendedor
            ADD CONSTRAINT fk_resena_vendedor_cliente FOREIGN KEY (cliente_id)
            REFERENCES xqm_seguridad.usuario(usuario_id) ON DELETE RESTRICT;
    END IF;
END $$;

ALTER TABLE xqm_soporte.reclamo
    ADD CONSTRAINT fk_reclamo_suborden FOREIGN KEY (suborden_id)
    REFERENCES xqm_venta.suborden(suborden_id) ON DELETE RESTRICT;

ALTER TABLE xqm_soporte.reclamo
    ADD CONSTRAINT fk_reclamo_abierto FOREIGN KEY (abierto_por)
    REFERENCES xqm_seguridad.usuario(usuario_id) ON DELETE RESTRICT;

ALTER TABLE xqm_soporte.reclamo
    ADD CONSTRAINT fk_reclamo_resuelto FOREIGN KEY (resuelto_por)
    REFERENCES xqm_seguridad.usuario(usuario_id) ON DELETE SET NULL;

ALTER TABLE xqm_soporte.mensaje_reclamo
    ADD CONSTRAINT fk_mensaje_reclamo FOREIGN KEY (reclamo_id)
    REFERENCES xqm_soporte.reclamo(reclamo_id) ON DELETE CASCADE;

ALTER TABLE xqm_soporte.mensaje_reclamo
    ADD CONSTRAINT fk_mensaje_remitente FOREIGN KEY (remitente_id)
    REFERENCES xqm_seguridad.usuario(usuario_id) ON DELETE RESTRICT;

ALTER TABLE xqm_soporte.notificacion
    ADD CONSTRAINT fk_notificacion_usuario FOREIGN KEY (usuario_id)
    REFERENCES xqm_seguridad.usuario(usuario_id) ON DELETE CASCADE;

DO $$
BEGIN
    IF to_regclass('xqm_soporte.mensaje_contacto') IS NOT NULL THEN
        ALTER TABLE xqm_soporte.mensaje_contacto
            ADD CONSTRAINT fk_mensaje_contacto_usuario FOREIGN KEY (usuario_id)
            REFERENCES xqm_seguridad.usuario(usuario_id) ON DELETE SET NULL;
        ALTER TABLE xqm_soporte.mensaje_contacto
            ADD CONSTRAINT fk_mensaje_contacto_respondido FOREIGN KEY (respondido_por)
            REFERENCES xqm_seguridad.usuario(usuario_id) ON DELETE SET NULL;
    END IF;
END $$;

ALTER TABLE xqm_catalogo.favorito
    ADD CONSTRAINT fk_favorito_usuario FOREIGN KEY (usuario_id)
    REFERENCES xqm_seguridad.usuario(usuario_id) ON DELETE CASCADE;

COMMIT;

-- Verificación rápida
SELECT 'persona.persona_id' AS col, data_type FROM information_schema.columns
 WHERE table_schema='xqm_seguridad' AND table_name='persona' AND column_name='persona_id'
UNION ALL
SELECT 'usuario.usuario_id', data_type FROM information_schema.columns
 WHERE table_schema='xqm_seguridad' AND table_name='usuario' AND column_name='usuario_id'
UNION ALL
SELECT 'orden.orden_id', data_type FROM information_schema.columns
 WHERE table_schema='xqm_venta' AND table_name='orden' AND column_name='orden_id'
UNION ALL
SELECT 'reclamo.reclamo_id', data_type FROM information_schema.columns
 WHERE table_schema='xqm_soporte' AND table_name='reclamo' AND column_name='reclamo_id';
