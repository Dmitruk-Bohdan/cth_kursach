INSERT INTO user_answer
(
    attempt_id,
    task_id,
    given_answer,
    is_correct,
    time_spent_sec,
    created_at,
    updated_at
)
VALUES
(
    @attempt_id,
    @task_id,
    @given_answer::jsonb,
    @is_correct,
    @time_spent_sec,
    NOW(),
    NOW()
)
ON CONFLICT (attempt_id, task_id)
DO UPDATE SET
    given_answer = EXCLUDED.given_answer,
    is_correct = EXCLUDED.is_correct,
    time_spent_sec = EXCLUDED.time_spent_sec,
    updated_at = NOW();
