INSERT INTO invitation_code (teacher_id, code, max_uses, used_count, expires_at, status, created_at)
VALUES (@teacher_id, @code, @max_uses, 0, @expires_at, @status, NOW())
RETURNING id;


