SELECT EXISTS(
    SELECT 1
    FROM user_sessions
    WHERE jti = @jti
      AND revoked_at IS NULL
      AND expires_at > NOW()
);
