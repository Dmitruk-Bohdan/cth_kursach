-- Получает темы с процентом успеха меньше критического значения
-- @user_id - ID пользователя
-- @subject_id - ID предмета
-- @critical_threshold - критический порог процента успеха (по умолчанию 80)
SELECT
    t.id as topic_id,
    t.topic_name,
    t.topic_code,
    COALESCE(us.attempts_total, 0) as attempts_total,
    COALESCE(us.correct_total, 0) as correct_total,
    CASE 
        WHEN COALESCE(us.attempts_total, 0) > 0 
        THEN (COALESCE(us.correct_total, 0)::NUMERIC / COALESCE(us.attempts_total, 1) * 100)
        ELSE NULL
    END as accuracy_percentage,
    us.last_attempt_at
FROM topic t
JOIN subject s ON s.id = t.subject_id
LEFT JOIN user_stats us ON us.user_id = @user_id 
    AND us.subject_id = @subject_id 
    AND us.topic_id = t.id
WHERE t.subject_id = @subject_id
  AND t.is_active = TRUE
  AND us.id IS NOT NULL  -- Только темы, по которым уже есть статистика
  AND us.attempts_total > 0  -- Только темы, по которым уже есть ответы
  AND (us.correct_total::NUMERIC / us.attempts_total * 100) < @critical_threshold  -- Процент успеха меньше критического
ORDER BY 
    CASE 
        WHEN COALESCE(us.attempts_total, 0) > 0 
        THEN (COALESCE(us.correct_total, 0)::NUMERIC / COALESCE(us.attempts_total, 1) * 100)
        ELSE 0
    END ASC,
    us.last_attempt_at DESC NULLS LAST;

