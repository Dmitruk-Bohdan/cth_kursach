INSERT INTO attempt
(
    test_id,
    user_id,
    assignment_id,
    started_at,
    status,
    created_at,
    updated_at
)
VALUES
(
    @test_id,
    @user_id,
    @assignment_id,
    NOW(),
    'in_progress',
    NOW(),
    NOW()
)
RETURNING id, started_at, status;
