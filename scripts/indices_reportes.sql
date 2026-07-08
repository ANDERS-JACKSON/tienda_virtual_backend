-- Índices de rendimiento para reportes admin (ejecutar una vez en PostgreSQL)
CREATE INDEX IF NOT EXISTS idx_orden_fecha ON xqm_venta.orden (fecha);
CREATE INDEX IF NOT EXISTS idx_orden_estado_fecha ON xqm_venta.orden (estado, fecha);
CREATE INDEX IF NOT EXISTS idx_transaccion_fecha ON xqm_pago.transaccion (fecha);
CREATE INDEX IF NOT EXISTS idx_transaccion_estado_fecha ON xqm_pago.transaccion (estado, fecha);
CREATE INDEX IF NOT EXISTS idx_suborden_estado_orden ON xqm_venta.suborden (estado, orden_id);
CREATE INDEX IF NOT EXISTS idx_item_orden_suborden ON xqm_venta.item_orden (suborden_id);
