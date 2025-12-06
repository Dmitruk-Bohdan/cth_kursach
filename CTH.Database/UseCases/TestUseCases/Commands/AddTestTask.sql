INSERT INTO test_task
(
    test_id,
    task_id,
    position,
    weight,
    created_at,
    updated_at
)
VALUES
(
    @test_id,
    @task_id,
    @position,
    @weight,
    NOW(),
    NOW()
)
ON CONFLICT (test_id, task_id) DO UPDATE SET
    position = EXCLUDED.position,
    weight = EXCLUDED.weight,
    updated_at = NOW();
