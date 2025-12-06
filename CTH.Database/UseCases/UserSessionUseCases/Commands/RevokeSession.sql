UPDATE user_sessions
SET revoked_at = @revoked_at
WHERE jti = @jti AND revoked_at IS NULL;
