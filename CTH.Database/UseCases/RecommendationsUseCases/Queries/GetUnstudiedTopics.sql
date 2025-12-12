-- Получает темы, которые не изучались (нет попыток с заданиями по этим темам)
-- @user_id - ID пользователя
-- @subject_id - ID предмета
SELECT
    t.id as topic_id,
    t.topic_name,
    t.topic_code
FROM topic t
JOIN subject s ON s.id = t.subject_id
WHERE t.subject_id = @subject_id
  AND t.is_active = TRUE
  AND NOT EXISTS (
      -- Проверяем, есть ли попытки с заданиями по этой теме
      SELECT 1
      FROM attempt a
      JOIN user_answer ua ON ua.attempt_id = a.id
      JOIN task_item ti ON ti.id = ua.task_id
      WHERE a.user_id = @user_id
        AND ti.topic_id = t.id
  )
ORDER BY t.topic_name;

