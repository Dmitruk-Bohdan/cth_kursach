INSERT INTO subject (subject_code, subject_name, is_active, created_at, updated_at)
VALUES (@subject_code, @subject_name, @is_active, NOW(), NOW())
RETURNING id;

