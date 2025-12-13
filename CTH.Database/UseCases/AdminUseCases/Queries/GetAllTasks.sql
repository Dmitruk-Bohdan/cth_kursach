SELECT 
    ti.id,
    ti.subject_id,
    s.subject_name,
    ti.topic_id,
    t.topic_name,
    ti.task_type,
    ti.difficulty,
    ti.statement,
    ti.correct_answer::text AS correct_answer,
    ti.explanation,
    ti.is_active,
    ti.created_at,
    ti.updated_at
FROM task_item ti
JOIN subject s ON s.id = ti.subject_id
LEFT JOIN topic t ON t.id = ti.topic_id
WHERE 
    (@subject_id IS NULL OR ti.subject_id = @subject_id)
    AND (@topic_id IS NULL OR ti.topic_id = @topic_id)
    AND (@task_type IS NULL OR ti.task_type = @task_type)
    AND (@difficulty IS NULL OR ti.difficulty = @difficulty)
    AND (@is_active IS NULL OR ti.is_active = @is_active)
    AND (@search IS NULL OR ti.statement ILIKE '%' || @search || '%')
ORDER BY ti.created_at DESC;

