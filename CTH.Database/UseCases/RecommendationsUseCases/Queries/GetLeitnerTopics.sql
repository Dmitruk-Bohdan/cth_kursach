-- Получает темы для повторения по системе Лейтнера
-- Система Лейтнера: интервалы повторения увеличиваются после успешного повторения
-- Интервалы: 1 день, 3 дня, 7 дней, 14 дней, 30 дней
-- Упрощенная версия: считаем количество успешных попыток (accuracy >= 80%) по теме
-- @user_id - ID пользователя
-- @subject_id - ID предмета
WITH topic_attempts AS (
    -- Получаем все попытки по темам с их accuracy
    SELECT
        t.id as topic_id,
        a.id as attempt_id,
        a.finished_at,
        (
            SELECT COUNT(*) FILTER (WHERE ua.is_correct = TRUE)::NUMERIC / NULLIF(COUNT(*), 0) * 100
            FROM user_answer ua
            JOIN task_item ti ON ti.id = ua.task_id
            WHERE ua.attempt_id = a.id AND ti.topic_id = t.id
        ) as attempt_accuracy
    FROM topic t
    JOIN attempt a ON a.user_id = @user_id AND a.status = 'completed'
    WHERE t.subject_id = @subject_id
      AND t.is_active = TRUE
      AND EXISTS (
          SELECT 1 
          FROM user_answer ua
          JOIN task_item ti ON ti.id = ua.task_id
          WHERE ua.attempt_id = a.id AND ti.topic_id = t.id
      )
),
topic_repetitions AS (
    -- Подсчитываем успешные повторения (accuracy >= 80%) для каждой темы
    SELECT
        topic_id,
        COUNT(*) FILTER (WHERE attempt_accuracy >= 80) as successful_repetitions,
        MAX(finished_at) as last_successful_attempt_at,
        MAX(finished_at) FILTER (WHERE attempt_accuracy >= 80) as last_attempt_at
    FROM topic_attempts
    GROUP BY topic_id
),
leitner_intervals AS (
    -- Определяем интервал повторения на основе количества успешных повторений
    SELECT
        tr.topic_id,
        t.topic_name,
        t.topic_code,
        tr.successful_repetitions,
        tr.last_attempt_at,
        CASE 
            WHEN tr.successful_repetitions = 0 THEN INTERVAL '1 day'
            WHEN tr.successful_repetitions = 1 THEN INTERVAL '3 days'
            WHEN tr.successful_repetitions = 2 THEN INTERVAL '7 days'
            WHEN tr.successful_repetitions = 3 THEN INTERVAL '14 days'
            WHEN tr.successful_repetitions >= 4 THEN INTERVAL '30 days'
            ELSE INTERVAL '1 day'
        END as repetition_interval
    FROM topic_repetitions tr
    JOIN topic t ON t.id = tr.topic_id
)
SELECT
    topic_id,
    topic_name,
    topic_code,
    successful_repetitions,
    last_attempt_at,
    EXTRACT(DAY FROM repetition_interval)::INTEGER as repetition_interval_days
FROM leitner_intervals
WHERE 
    -- Нужно повторить, если прошло больше интервала или еще не было успешных попыток
    last_attempt_at IS NULL 
    OR last_attempt_at + repetition_interval <= NOW()
ORDER BY 
    last_attempt_at ASC NULLS FIRST,  -- Сначала те, что давно не повторяли
    successful_repetitions ASC;  -- Затем по количеству успешных повторений

