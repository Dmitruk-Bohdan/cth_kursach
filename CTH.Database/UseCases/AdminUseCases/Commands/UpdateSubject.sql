UPDATE subject
SET
    subject_code = COALESCE(@subject_code, subject_code),
    subject_name = COALESCE(@subject_name, subject_name),
    is_active = COALESCE(@is_active, is_active),
    updated_at = NOW()
WHERE id = @subject_id
RETURNING id;

