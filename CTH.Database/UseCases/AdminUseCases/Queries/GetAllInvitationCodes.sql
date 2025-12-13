SELECT 
    ic.id,
    ic.teacher_id,
    ua.user_name AS teacher_name,
    ua.email AS teacher_email,
    ic.code,
    ic.max_uses,
    ic.used_count,
    ic.expires_at,
    ic.status,
    ic.created_at
FROM invitation_code ic
JOIN user_account ua ON ua.id = ic.teacher_id
WHERE (@teacher_id IS NULL OR ic.teacher_id = @teacher_id)
    AND (@status IS NULL OR ic.status = @status)
ORDER BY ic.created_at DESC;

