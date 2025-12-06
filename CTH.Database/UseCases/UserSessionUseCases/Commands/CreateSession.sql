INSERT INTO user_sessions
(
    user_id,
    jti,
    created_at,
    expires_at
)
VALUES
(
    @user_id,
    @jti,
    @created_at,
    @expires_at
);
