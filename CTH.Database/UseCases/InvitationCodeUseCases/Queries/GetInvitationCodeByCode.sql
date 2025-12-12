SELECT
    id,
    teacher_id,
    code,
    max_uses,
    used_count,
    expires_at,
    status,
    created_at
FROM invitation_code
WHERE code = @code;

