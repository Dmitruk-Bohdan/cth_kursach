SELECT
    id,
    user_name,
    email,
    password_hash,
    role_type_id,
    last_login_at,
    created_at,
    updated_at
FROM user_account
WHERE email = @email
LIMIT 1;
