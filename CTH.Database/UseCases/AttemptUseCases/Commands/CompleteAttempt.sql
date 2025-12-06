UPDATE attempt
SET
    status = 'completed',
    finished_at = NOW(),
    raw_score = @raw_score,
    scaled_score = @scaled_score,
    duration_sec = @duration_sec,
    updated_at = NOW()
WHERE id = @attempt_id AND user_id = @user_id;
