UPDATE test
SET
    subject_id = @subject_id,
    test_kind = @test_kind,
    title = @title,
    time_limit_sec = @time_limit_sec,
    attempts_allowed = @attempts_allowed,
    mode = @mode,
    is_published = @is_published,
    is_public = @is_public,
    is_state_archive = @is_state_archive,
    updated_at = NOW()
WHERE id = @test_id;
