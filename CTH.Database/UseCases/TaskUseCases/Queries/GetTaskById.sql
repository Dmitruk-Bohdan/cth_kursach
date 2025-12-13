-- Получает задание по ID
-- @task_id - ID задания
SELECT
    ti.id,
    ti.subject_id,
    ti.topic_id,
    ti.task_type,
    ti.difficulty,
    ti.statement,
    ti.correct_answer,
    ti.explanation,
    ti.is_active,
    t.topic_name,
    t.topic_code
FROM task_item ti
LEFT JOIN topic t ON t.id = ti.topic_id
WHERE ti.id = @task_id;

