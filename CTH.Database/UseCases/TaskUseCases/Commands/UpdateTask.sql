-- Обновляет задание
-- @task_id - ID задания
-- @topic_id - ID темы (может быть NULL для обновления, NULL означает не обновлять)
-- @task_type - тип задания (NULL означает не обновлять)
-- @difficulty - сложность (1-5, NULL означает не обновлять)
-- @statement - условие задания (NULL означает не обновлять)
-- @correct_answer - правильный ответ (JSON, NULL означает не обновлять)
-- @explanation - объяснение (NULL означает не обновлять)
-- @is_active - активность задания (NULL означает не обновлять)
UPDATE task_item
SET
    topic_id = CASE WHEN @topic_id IS NULL THEN topic_id ELSE @topic_id END,
    task_type = CASE WHEN @task_type IS NULL THEN task_type ELSE @task_type END,
    difficulty = CASE WHEN @difficulty IS NULL THEN difficulty ELSE @difficulty END,
    statement = CASE WHEN @statement IS NULL THEN statement ELSE @statement END,
    correct_answer = CASE WHEN @correct_answer IS NULL THEN correct_answer ELSE @correct_answer::jsonb END,
    explanation = CASE WHEN @explanation IS NULL THEN explanation ELSE @explanation END,
    is_active = CASE WHEN @is_active IS NULL THEN is_active ELSE @is_active END,
    updated_at = NOW()
WHERE id = @task_id
RETURNING id;

