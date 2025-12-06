CREATE TABLE IF NOT EXISTS user_sessions
(
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL,
    jti UUID NOT NULL UNIQUE,
    created_at TIMESTAMPTZ NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    revoked_at TIMESTAMPTZ NULL,
    CONSTRAINT fk_user_session_user
        FOREIGN KEY (user_id) REFERENCES user_account (id)
        ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_user_sessions_jti ON user_sessions (jti);
