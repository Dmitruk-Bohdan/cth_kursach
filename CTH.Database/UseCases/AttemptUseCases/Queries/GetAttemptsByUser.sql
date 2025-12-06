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
    t.title AS test_title,
    t.subject_id,
    s.subject_name
FROM attempt a
JOIN test t ON t.id = a.test_id
JOIN subject s ON s.id = t.subject_id
WHERE a.user_id = @user_id
  AND (@status IS NULL OR a.status = @status)
ORDER BY a.started_at DESC
LIMIT @limit OFFSET @offset;

