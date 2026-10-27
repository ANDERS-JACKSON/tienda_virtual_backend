-- Cobros de suscripción con motivo (renovación, pago anticipado, upgrade) y plan destino.
ALTER TABLE xqm_pago.transaccion
    ADD COLUMN IF NOT EXISTS motivo_suscripcion int2 NULL,
    ADD COLUMN IF NOT EXISTS plan_destino_id integer NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'fk_transaccion_plan_destino'
    ) THEN
        ALTER TABLE xqm_pago.transaccion
            ADD CONSTRAINT fk_transaccion_plan_destino
            FOREIGN KEY (plan_destino_id)
            REFERENCES xqm_vendedor.plan (plan_id)
            ON DELETE RESTRICT;
    END IF;
END $$;

-- Aviso de vencimiento (7 días antes); se limpia al extender el periodo.
ALTER TABLE xqm_vendedor.suscripcion
    ADD COLUMN IF NOT EXISTS aviso_vencimiento_enviado_en timestamp with time zone NULL;
