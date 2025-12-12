-- Миграция: увеличение размера поля code в таблице invitation_code с 32 до 36 символов
-- для поддержки формата GUID: XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX

ALTER TABLE invitation_code
ALTER COLUMN code TYPE VARCHAR(36);

