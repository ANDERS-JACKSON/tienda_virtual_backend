-- Agregar columna motivo_pausa_admin a producto (Módulo 3 - moderación admin)
ALTER TABLE xqm_catalogo.producto
ADD COLUMN IF NOT EXISTS motivo_pausa_admin varchar(500) NULL;
