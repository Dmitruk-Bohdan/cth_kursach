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
  AND (
        t.is_public = TRUE
     OR t.is_state_archive = TRUE
     OR (@user_id IS NOT NULL AND EXISTS (
            SELECT 1 FROM teacher_student ts
            WHERE ts.teacher_id = t.author_id
              AND ts.student_id = @user_id
              AND ts.status IN ('active', 'approved')
        ))
  )
  AND (@subject_id IS NULL OR t.subject_id = @subject_id)
  AND (@only_teachers = FALSE OR (@user_id IS NOT NULL AND EXISTS (
        SELECT 1 FROM teacher_student ts
        WHERE ts.teacher_id = t.author_id
          AND ts.student_id = @user_id
          AND ts.status IN ('active', 'approved')
      )))
  AND (@only_state_archive = FALSE OR t.is_state_archive = TRUE)
  AND (@only_limited_attempts = FALSE OR t.attempts_allowed IS NOT NULL)
  AND (@title_pattern IS NULL OR t.title ILIKE @title_pattern)
  AND (@mode IS NULL OR t.mode = @mode)
ORDER BY t.updated_at DESC;
