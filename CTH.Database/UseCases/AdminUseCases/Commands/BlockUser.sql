-- Блокировка через удаление всех сессий
UPDATE user_sessions
SET revoked_at = NOW()
WHERE user_id = @user_id
  AND revoked_at IS NULL;

