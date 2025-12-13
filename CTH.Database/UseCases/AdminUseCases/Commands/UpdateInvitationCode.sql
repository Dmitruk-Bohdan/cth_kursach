UPDATE invitation_code
SET 
    max_uses = CASE WHEN @max_uses IS NULL THEN max_uses ELSE @max_uses END,
    expires_at = CASE WHEN @expires_at IS NULL THEN expires_at ELSE @expires_at END,
    status = CASE WHEN @status IS NULL THEN status ELSE @status END
WHERE id = @id
RETURNING id;

