-- WhatsApp de contacto público por tienda (independiente de Yape).
-- Idempotente: seguro re-ejecutar.

ALTER TABLE xqm_vendedor.vendedor
    ADD COLUMN IF NOT EXISTS numero_whatsapp varchar(20) NULL;

COMMENT ON COLUMN xqm_vendedor.vendedor.numero_whatsapp IS
    'Celular WhatsApp público de la tienda (9 dígitos Perú, sin +51).';
