UPDATE user_account
SET
    user_name = COALESCE(@user_name, user_name),
    email = COALESCE(@email, email),
    password_hash = COALESCE(@password_hash, password_hash),
    role_type_id = COALESCE(@role_type_id, role_type_id),
    updated_at = NOW()
WHERE id = @user_id
RETURNING id;

