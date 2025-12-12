-- Получает все активные темы по предмету (без дубликатов)
SELECT DISTINCT ON (t.id)
    t.id,
    t.topic_name,
    t.topic_code,
    t.subject_id,
    t.is_active,
    t.created_at,
    t.updated_at
FROM topic t
WHERE t.subject_id = @subject_id
  AND t.is_active = TRUE
ORDER BY t.id, t.topic_name;

