INSERT INTO task_item
(
    subject_id,
    topic_id,
    task_type,
    difficulty,
    statement,
    correct_answer,
    explanation,
    is_active,
    created_at,
    updated_at
)
VALUES
(
    @subject_id,
    @topic_id,
    @task_type,
    @difficulty,
    @statement,
    @correct_answer::jsonb,
    @explanation,
    @is_active,
    NOW(),
    NOW()
)
RETURNING id;

