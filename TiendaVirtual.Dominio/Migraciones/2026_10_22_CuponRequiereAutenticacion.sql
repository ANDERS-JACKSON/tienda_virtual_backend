-- Cupón de pedido: si requiere_autenticacion es true, solo se aplica con sesión.
ALTER TABLE xqm_venta.cupon_pedido
    ADD COLUMN IF NOT EXISTS requiere_autenticacion boolean NOT NULL DEFAULT false;
