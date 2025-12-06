INSERT INTO user_account
    (
        user_name,
        email,
        password_hash,
        role_type_id,
        last_login_at,
        created_at,
        updated_at
    )
VALUES
    (
        @user_name,
        @email,
        @password_hash,
        @role_type_id,
        @last_login_at,
        @created_at,
        @updated_at
    )
RETURNING id;
