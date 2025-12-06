UPDATE attempt
SET
    status = 'in_progress',
    finished_at = NULL,
    updated_at = NOW()
WHERE id = @attempt_id 
  AND user_id = @user_id 
  AND status = 'aborted';

