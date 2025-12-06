SELECT
    id,
    attempt_id,
    task_id,
    given_answer,
    is_correct,
    time_spent_sec,
    created_at,
    updated_at
FROM user_answer
WHERE attempt_id = @attempt_id
ORDER BY task_id;
