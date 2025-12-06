SELECT
    t.id,
    t.subject_id,
    t.test_kind,
    t.title,
    t.author_id,
    t.time_limit_sec,
    t.attempts_allowed,
    t.mode,
    t.is_published,
    t.is_public,
    t.is_state_archive,
    t.created_at,
    t.updated_at,
    s.subject_name
FROM test t
JOIN subject s ON s.id = t.subject_id
WHERE t.is_published = TRUE
  AND (t.is_public = TRUE OR t.is_state_archive = TRUE)
  AND (@subject_id IS NULL OR t.subject_id = @subject_id)
ORDER BY t.updated_at DESC;
