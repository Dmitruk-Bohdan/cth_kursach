INSERT INTO invitation_code (teacher_id, code, max_uses, expires_at, status)
VALUES (@teacher_id, @code, @max_uses, @expires_at, @status)
RETURNING id;

