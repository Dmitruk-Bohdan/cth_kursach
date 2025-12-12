SELECT
    ts.id,
    ts.teacher_id,
    ts.student_id,
    ts.status,
    ts.established_at,
    ts.created_at,
    ts.updated_at,
    u.id as user_id,
    u.user_name,
    u.email
FROM teacher_student ts
JOIN user_account u ON u.id = ts.student_id
WHERE ts.teacher_id = @teacher_id
  AND ts.status IN ('active', 'approved')
ORDER BY ts.established_at DESC NULLS LAST, ts.created_at DESC;

