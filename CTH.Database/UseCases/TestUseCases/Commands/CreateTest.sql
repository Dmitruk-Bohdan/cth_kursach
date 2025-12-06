INSERT INTO test
(
    subject_id,
    test_kind,
    title,
    author_id,
    time_limit_sec,
    attempts_allowed,
    mode,
    is_published,
    created_at,
    updated_at
)
VALUES
(
    @subject_id,
    @test_kind,
    @title,
    @author_id,
    @time_limit_sec,
    @attempts_allowed,
    @mode,
    @is_published,
    NOW(),
    NOW()
)
RETURNING id;
