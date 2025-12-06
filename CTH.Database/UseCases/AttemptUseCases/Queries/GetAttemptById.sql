SELECT
    a.id,
    a.test_id,
    a.user_id,
    a.assignment_id,
    a.started_at,
    a.finished_at,
    a.status,
    a.raw_score,
    a.scaled_score,
    a.duration_sec,
    a.created_at,
    a.updated_at,
    t.title,
    t.subject_id
FROM attempt a
JOIN test t ON t.id = a.test_id
WHERE a.id = @attempt_id AND a.user_id = @user_id;
