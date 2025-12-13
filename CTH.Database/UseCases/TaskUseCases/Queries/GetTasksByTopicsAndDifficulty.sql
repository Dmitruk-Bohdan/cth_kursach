-- Получает задания по списку тем и сложности
-- @topic_ids - массив ID тем
-- @difficulties - массив уровней сложности (1-5)
-- @limit_per_topic - максимальное количество заданий на тему
SELECT
    ti.id,
    ti.subject_id,
    ti.topic_id,
    ti.task_type,
    ti.difficulty,
    ti.statement,
    ti.explanation,
    ti.is_active,
    t.topic_name,
    t.topic_code
FROM task_item ti
JOIN topic t ON t.id = ti.topic_id
WHERE ti.topic_id = ANY(@topic_ids::BIGINT[])
  AND ti.difficulty = ANY(@difficulties::SMALLINT[])
  AND ti.is_active = TRUE
ORDER BY 
    ti.topic_id,
    random()  -- Случайный порядок для разнообразия
LIMIT @limit_per_topic * array_length(@topic_ids::BIGINT[], 1);

