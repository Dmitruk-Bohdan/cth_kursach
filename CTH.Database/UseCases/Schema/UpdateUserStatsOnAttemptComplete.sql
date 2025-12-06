-- Триггер для автоматического обновления статистики при завершении попытки
-- Вызывается при изменении статуса попытки на 'completed'

-- Создаем уникальный индекс для user_stats (если его еще нет в CreateAllTables.sql)
-- Используем выражение для обработки NULL значений
CREATE UNIQUE INDEX IF NOT EXISTS idx_user_stats_unique 
ON user_stats (user_id, COALESCE(subject_id, 0), COALESCE(topic_id, 0));

-- Функция триггера
CREATE OR REPLACE FUNCTION update_user_stats_on_attempt_complete()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    -- Вызываем процедуру расчета статистики только если статус изменился на 'completed'
    IF NEW.status = 'completed' AND (OLD.status IS NULL OR OLD.status != 'completed') THEN
        PERFORM calculate_user_statistics(NEW.user_id, NEW.id);
    END IF;
    
    RETURN NEW;
END;
$$;

-- Создаем триггер
DROP TRIGGER IF EXISTS trigger_update_user_stats_on_attempt_complete ON attempt;
CREATE TRIGGER trigger_update_user_stats_on_attempt_complete
    AFTER UPDATE OF status ON attempt
    FOR EACH ROW
    WHEN (NEW.status = 'completed' AND (OLD.status IS NULL OR OLD.status != 'completed'))
    EXECUTE FUNCTION update_user_stats_on_attempt_complete();

