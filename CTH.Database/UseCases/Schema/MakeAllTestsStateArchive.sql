-- Скрипт для преобразования всех существующих тестов в государственные
-- Устанавливает:
-- - is_state_archive = TRUE
-- - author_id = NULL (государственные тесты не имеют автора)
-- - test_kind = 'PAST_EXAM' (если не установлен)
-- - is_public = TRUE (государственные тесты публичные)

UPDATE test
SET 
    is_state_archive = TRUE,
    author_id = NULL,
    test_kind = CASE 
        WHEN test_kind NOT IN ('PAST_EXAM', 'CUSTOM', 'MIXED') THEN 'PAST_EXAM'
        ELSE test_kind
    END,
    is_public = TRUE,
    updated_at = NOW()
WHERE is_state_archive = FALSE OR author_id IS NOT NULL;

