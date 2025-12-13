UPDATE task_item
SET is_active = TRUE, updated_at = NOW()
WHERE id = @task_id
RETURNING id;

