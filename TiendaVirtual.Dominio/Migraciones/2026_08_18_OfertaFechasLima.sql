-- Corrige ofertas guardadas como medianoche UTC (se veían un día antes en Perú).

UPDATE xqm_catalogo.oferta
SET fecha_inicio = fecha_inicio + INTERVAL '5 hours'
WHERE (fecha_inicio AT TIME ZONE 'UTC')::time = TIME '00:00:00';

UPDATE xqm_catalogo.oferta
SET fecha_fin = (
        ((fecha_fin AT TIME ZONE 'UTC')::date + INTERVAL '1 day')
        AT TIME ZONE 'America/Lima'
    ) - INTERVAL '1 microsecond'
WHERE (fecha_fin AT TIME ZONE 'UTC')::time >= TIME '23:59:59';
