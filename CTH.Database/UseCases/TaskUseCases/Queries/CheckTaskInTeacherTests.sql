-- Проверяет, используется ли задание в тестах преподавателя
-- @task_id - ID задания
-- @teacher_id - ID преподавателя
SELECT EXISTS (
    SELECT 1
    FROM test_task tt
    JOIN test t ON t.id = tt.test_id
    WHERE tt.task_id = @task_id
      AND t.author_id = @teacher_id
) AS is_used;

