UPDATE user_account
SET
    last_login_at = @last_login_at,
    updated_at = @updated_at
WHERE id = @id;
