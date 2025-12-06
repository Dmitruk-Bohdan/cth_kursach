UPDATE attempt
SET
    status = 'aborted',
    finished_at = NOW(),
    updated_at = NOW()
WHERE id = @attempt_id AND user_id = @user_id AND status = 'in_progress';

