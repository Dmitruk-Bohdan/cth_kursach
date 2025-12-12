SELECT
    s.id,
    s.subject_code,
    s.subject_name,
    s.is_active,
    s.created_at,
    s.updated_at
FROM subject s
WHERE s.is_active = TRUE
ORDER BY s.subject_name;

