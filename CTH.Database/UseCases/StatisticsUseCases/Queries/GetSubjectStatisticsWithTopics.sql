-- Получает статистику по предмету с разбивкой по темам
-- Возвращает:
-- 1. Общую статистику по предмету (subject_id, topic_id = NULL)
-- 2. Статистику по темам с ошибками (отсортированные)
-- 3. Темы без статистики (не решал тесты)

WITH subject_stats AS (
    -- Общая статистика по предмету
    SELECT 
        us.id,
        us.user_id,
        us.subject_id,
        NULL::BIGINT as topic_id,
        us.attempts_total,
        us.correct_total,
        us.last_attempt_at,
        us.average_score,
        us.average_time_sec,
        us.created_at,
        us.updated_at,
        s.subject_name,
        NULL::VARCHAR as topic_name,
        CASE 
            WHEN us.attempts_total > 0 
            THEN (us.correct_total::NUMERIC / us.attempts_total * 100)
            ELSE NULL
        END as accuracy_percentage,
        (us.attempts_total - us.correct_total) as errors_count
    FROM user_stats us
    JOIN subject s ON s.id = us.subject_id
    WHERE us.user_id = @user_id
      AND us.subject_id = @subject_id
      AND us.topic_id IS NULL
),
topic_stats AS (
    -- Статистика по темам с ошибками
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
        t.topic_name,
        CASE 
            WHEN us.attempts_total > 0 
            THEN (us.correct_total::NUMERIC / us.attempts_total * 100)
            ELSE NULL
        END as accuracy_percentage,
        (us.attempts_total - us.correct_total) as errors_count
    FROM user_stats us
    JOIN subject s ON s.id = us.subject_id
    JOIN topic t ON t.id = us.topic_id
    WHERE us.user_id = @user_id
      AND us.subject_id = @subject_id
      AND us.topic_id IS NOT NULL
),
top_3_errors AS (
    -- Топ 3 темы с наибольшим количеством ошибок
    SELECT *
    FROM topic_stats
    ORDER BY errors_count DESC, accuracy_percentage ASC NULLS LAST
    LIMIT 3
),
other_topics AS (
    -- Остальные темы, отсортированные по возрастанию процента успешности
    SELECT *
    FROM topic_stats
    WHERE topic_id NOT IN (SELECT topic_id FROM top_3_errors WHERE topic_id IS NOT NULL)
    ORDER BY accuracy_percentage ASC NULLS LAST, errors_count DESC
),
topics_without_stats AS (
    -- Темы, по которым нет статистики
    SELECT 
        NULL::BIGINT as id,
        @user_id as user_id,
        s.id as subject_id,
        t.id as topic_id,
        0 as attempts_total,
        0 as correct_total,
        NULL::TIMESTAMPTZ as last_attempt_at,
        NULL::NUMERIC as average_score,
        NULL::INTEGER as average_time_sec,
        NOW() as created_at,
        NOW() as updated_at,
        s.subject_name,
        t.topic_name,
        NULL::NUMERIC as accuracy_percentage,
        0 as errors_count
    FROM topic t
    JOIN subject s ON s.id = t.subject_id
    WHERE t.subject_id = @subject_id
      AND t.is_active = TRUE
      AND t.id NOT IN (
          SELECT DISTINCT topic_id 
          FROM user_stats 
          WHERE user_id = @user_id 
            AND subject_id = @subject_id 
            AND topic_id IS NOT NULL
      )
)
-- Объединяем все результаты
SELECT 
    id,
    user_id,
    subject_id,
    topic_id,
    attempts_total,
    correct_total,
    last_attempt_at,
    average_score,
    average_time_sec,
    created_at,
    updated_at,
    subject_name,
    topic_name,
    accuracy_percentage,
    errors_count
FROM (
    SELECT *, 0 as sort_order FROM subject_stats
    UNION ALL
    SELECT *, 1 as sort_order FROM top_3_errors
    UNION ALL
    SELECT *, 2 as sort_order FROM other_topics
    UNION ALL
    SELECT *, 3 as sort_order FROM topics_without_stats
) combined
ORDER BY 
    combined.sort_order,
    combined.errors_count DESC,  -- Внутри топ-3 сортируем по ошибкам
    combined.accuracy_percentage ASC NULLS LAST;  -- Остальные по проценту успешности

