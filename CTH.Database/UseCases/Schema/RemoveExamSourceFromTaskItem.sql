-- Удаление колонки exam_source_id из таблицы task_item
-- Это удалит внешний ключ и саму колонку

ALTER TABLE task_item
DROP CONSTRAINT IF EXISTS task_item_exam_source_id_fkey;

ALTER TABLE task_item
DROP COLUMN IF EXISTS exam_source_id;

