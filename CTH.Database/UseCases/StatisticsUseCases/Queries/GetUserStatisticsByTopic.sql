SELECT
    us.id,
    us.user_id,
    us.subject_id,
    us.topic_id,
    us.attempts_total,
    us.correct_total,
    us.last_attempt_at,
    us.average_score,
    us.average_time_sec,
    us.created_at,
    us.updated_at,
    s.subject_name,
    t.topic_name
FROM user_stats us
LEFT JOIN subject s ON s.id = us.subject_id
LEFT JOIN topic t ON t.id = us.topic_id
WHERE us.user_id = @user_id
  AND (@subject_id IS NULL OR us.subject_id = @subject_id)
  AND us.topic_id IS NOT NULL
ORDER BY s.subject_name, t.topic_name;

