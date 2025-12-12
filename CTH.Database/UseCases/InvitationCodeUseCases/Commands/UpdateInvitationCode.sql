UPDATE invitation_code
SET status = @status,
    used_count = @used_count
WHERE id = @id;

