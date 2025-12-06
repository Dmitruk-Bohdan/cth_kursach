-- Процедура для расчета статистики пользователя
-- Обновляет или создает записи в user_stats на основе завершенных попыток
CREATE OR REPLACE FUNCTION calculate_user_statistics(p_user_id BIGINT, p_attempt_id BIGINT)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_test_id BIGINT;
    v_subject_id BIGINT;
    v_raw_score NUMERIC(6,2);
    v_duration_sec INTEGER;
    v_finished_at TIMESTAMPTZ;
    v_correct_count INTEGER;
    v_total_count INTEGER;
    v_topic_id BIGINT;
BEGIN
    -- Получаем информацию о завершенной попытке
    SELECT 
        a.test_id,
        t.subject_id,
        a.raw_score,
        a.duration_sec,
        a.finished_at
    INTO 
        v_test_id,
        v_subject_id,
        v_raw_score,
        v_duration_sec,
        v_finished_at
    FROM attempt a
    JOIN test t ON t.id = a.test_id
    WHERE a.id = p_attempt_id
      AND a.user_id = p_user_id
      AND a.status = 'completed';
    
    -- Если попытка не найдена или не завершена, выходим
    IF v_test_id IS NULL THEN
        RETURN;
    END IF;
    
    -- Подсчитываем правильные и общие ответы для попытки
    SELECT 
        COUNT(*) FILTER (WHERE ua.is_correct = TRUE),
        COUNT(*)
    INTO 
        v_correct_count,
        v_total_count
    FROM user_answer ua
    WHERE ua.attempt_id = p_attempt_id;
    
    -- Обновляем статистику по предмету (subject_id, topic_id = NULL)
    -- Используем INSERT ... ON CONFLICT с уникальным индексом
    INSERT INTO user_stats (user_id, subject_id, topic_id, attempts_total, correct_total, last_attempt_at, average_score, average_time_sec, updated_at)
    VALUES (p_user_id, v_subject_id, NULL, 1, v_correct_count, v_finished_at, v_raw_score, v_duration_sec, NOW())
    ON CONFLICT (user_id, COALESCE(subject_id, 0), COALESCE(topic_id, 0))
    DO UPDATE SET
        attempts_total = user_stats.attempts_total + 1,
        correct_total = user_stats.correct_total + v_correct_count,
        last_attempt_at = GREATEST(COALESCE(user_stats.last_attempt_at, '1970-01-01'::timestamptz), v_finished_at),
        average_score = (
            SELECT AVG(a2.raw_score)
            FROM attempt a2
            JOIN test t2 ON t2.id = a2.test_id
            WHERE a2.user_id = p_user_id
              AND a2.status = 'completed'
              AND t2.subject_id = v_subject_id
        ),
        average_time_sec = (
            SELECT AVG(a2.duration_sec)::INTEGER
            FROM attempt a2
            JOIN test t2 ON t2.id = a2.test_id
            WHERE a2.user_id = p_user_id
              AND a2.status = 'completed'
              AND t2.subject_id = v_subject_id
              AND a2.duration_sec IS NOT NULL
        ),
        updated_at = NOW();
    
    -- Обновляем статистику по темам для каждого задания в попытке
    FOR v_topic_id IN
        SELECT DISTINCT ti.topic_id
        FROM user_answer ua
        JOIN task_item ti ON ti.id = ua.task_id
        WHERE ua.attempt_id = p_attempt_id
          AND ti.topic_id IS NOT NULL
    LOOP
        -- Подсчитываем правильные ответы по теме в этой попытке
        SELECT 
            COUNT(*) FILTER (WHERE ua.is_correct = TRUE)
        INTO v_correct_count
        FROM user_answer ua
        JOIN task_item ti ON ti.id = ua.task_id
        WHERE ua.attempt_id = p_attempt_id
          AND ti.topic_id = v_topic_id;
        
        -- Обновляем статистику по теме
        INSERT INTO user_stats (user_id, subject_id, topic_id, attempts_total, correct_total, last_attempt_at, average_score, average_time_sec, updated_at)
        VALUES (p_user_id, v_subject_id, v_topic_id, 1, v_correct_count, v_finished_at, NULL, NULL, NOW())
        ON CONFLICT (user_id, COALESCE(subject_id, 0), COALESCE(topic_id, 0))
        DO UPDATE SET
            attempts_total = user_stats.attempts_total + 1,
            correct_total = user_stats.correct_total + v_correct_count,
            last_attempt_at = GREATEST(COALESCE(user_stats.last_attempt_at, '1970-01-01'::timestamptz), v_finished_at),
            updated_at = NOW();
    END LOOP;
END;
$$;

