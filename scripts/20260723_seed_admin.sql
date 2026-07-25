-- ============================================================
-- Solo crea el usuario ADMINISTRADOR (persona + usuario + rol).
-- Compatible con PKs uuid (migración 20260723).
-- Idempotente: si el correo ya existe, no duplica.
-- ============================================================
-- Credenciales por defecto:
--   Correo:      admin@artesanias.com
--   Contraseña:  Admin123!
-- ============================================================

CREATE EXTENSION IF NOT EXISTS pgcrypto;

BEGIN;

-- Asegura el rol ADMIN (rol_id = 1 según TipoRol en C#)
INSERT INTO xqm_seguridad.rol (rol_id, nombre, descripcion)
VALUES (1, 'ADMIN', 'Administrador del sistema')
ON CONFLICT (rol_id) DO UPDATE
SET nombre = EXCLUDED.nombre,
    descripcion = EXCLUDED.descripcion;

DO $$
DECLARE
    v_correo           citext := 'admin@artesanias.com';
    -- BCrypt de "Admin123!" (compatible con BCrypt.Net)
    v_hash             varchar := '$2b$11$YQQlRA6FdIaMKUmKSt0r3.c7RvYb7JUnyza2itKPzJWO28ZtXl732';
    v_persona_id       uuid;
    v_usuario_id       uuid;
BEGIN
    IF EXISTS (
        SELECT 1 FROM xqm_seguridad.usuario WHERE correo = v_correo
    ) THEN
        RAISE NOTICE 'El administrador % ya existe. No se creó nada nuevo.', v_correo;
        RETURN;
    END IF;

    v_persona_id := gen_random_uuid();
    v_usuario_id := gen_random_uuid();

    INSERT INTO xqm_seguridad.persona (
        persona_id,
        tipo_documento,      -- 1 = DNI
        numero_documento,
        apellido_paterno,
        apellido_materno,
        nombres,
        sexo,
        telefono,
        correo_electronico
    ) VALUES (
        v_persona_id,
        1,
        '00000000',
        'Sistema',
        'Admin',
        'Administrador',
        NULL,
        NULL,
        v_correo
    );

    INSERT INTO xqm_seguridad.usuario (
        usuario_id,
        persona_id,
        correo,
        contrasena,
        correo_confirmado,
        forzar_cambio_clave,
        estado,              -- 1 = Activo
        fecha_alta,
        two_factor_enabled
    ) VALUES (
        v_usuario_id,
        v_persona_id,
        v_correo,
        v_hash,
        true,
        false,
        1,
        now(),
        false
    );

    INSERT INTO xqm_seguridad.usuario_rol (usuario_id, rol_id)
    VALUES (v_usuario_id, 1);  -- ADMIN

    RAISE NOTICE 'Administrador creado: % / Admin123!', v_correo;
END $$;

COMMIT;
