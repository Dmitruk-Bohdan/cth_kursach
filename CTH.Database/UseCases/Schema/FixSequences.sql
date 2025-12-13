-- Скрипт для синхронизации последовательностей (sequences) с текущими максимальными значениями ID
-- Это необходимо, если данные были вставлены вручную с явным указанием ID
-- Выполните этот скрипт после ручной вставки данных или если возникают ошибки duplicate key

-- Синхронизация последовательности для user_account
DO $$
DECLARE
    max_id BIGINT;
    seq_name TEXT;
BEGIN
    -- Получаем имя последовательности для user_account.id
    SELECT pg_get_serial_sequence('user_account', 'id') INTO seq_name;
    
    -- Получаем максимальный ID
    SELECT COALESCE(MAX(id), 0) FROM user_account INTO max_id;
    
    -- Устанавливаем значение последовательности на максимальный ID + 1
    IF seq_name IS NOT NULL THEN
        EXECUTE format('SELECT setval(%L, %s, true)', seq_name, GREATEST(max_id, 1));
        RAISE NOTICE 'Synchronized sequence % to %', seq_name, GREATEST(max_id, 1);
    END IF;
END $$;

-- Синхронизация последовательности для role
DO $$
DECLARE
    max_id BIGINT;
    seq_name TEXT;
BEGIN
    SELECT pg_get_serial_sequence('role', 'id') INTO seq_name;
    SELECT COALESCE(MAX(id), 0) FROM role INTO max_id;
    IF seq_name IS NOT NULL THEN
        EXECUTE format('SELECT setval(%L, %s, true)', seq_name, GREATEST(max_id, 1));
        RAISE NOTICE 'Synchronized sequence % to %', seq_name, GREATEST(max_id, 1);
    END IF;
END $$;

-- Синхронизация последовательности для subject
DO $$
DECLARE
    max_id BIGINT;
    seq_name TEXT;
BEGIN
    SELECT pg_get_serial_sequence('subject', 'id') INTO seq_name;
    SELECT COALESCE(MAX(id), 0) FROM subject INTO max_id;
    IF seq_name IS NOT NULL THEN
        EXECUTE format('SELECT setval(%L, %s, true)', seq_name, GREATEST(max_id, 1));
        RAISE NOTICE 'Synchronized sequence % to %', seq_name, GREATEST(max_id, 1);
    END IF;
END $$;

-- Синхронизация последовательности для topic
DO $$
DECLARE
    max_id BIGINT;
    seq_name TEXT;
BEGIN
    SELECT pg_get_serial_sequence('topic', 'id') INTO seq_name;
    SELECT COALESCE(MAX(id), 0) FROM topic INTO max_id;
    IF seq_name IS NOT NULL THEN
        EXECUTE format('SELECT setval(%L, %s, true)', seq_name, GREATEST(max_id, 1));
        RAISE NOTICE 'Synchronized sequence % to %', seq_name, GREATEST(max_id, 1);
    END IF;
END $$;

-- Синхронизация последовательности для task_item
DO $$
DECLARE
    max_id BIGINT;
    seq_name TEXT;
BEGIN
    SELECT pg_get_serial_sequence('task_item', 'id') INTO seq_name;
    SELECT COALESCE(MAX(id), 0) FROM task_item INTO max_id;
    IF seq_name IS NOT NULL THEN
        EXECUTE format('SELECT setval(%L, %s, true)', seq_name, GREATEST(max_id, 1));
        RAISE NOTICE 'Synchronized sequence % to %', seq_name, GREATEST(max_id, 1);
    END IF;
END $$;

-- Синхронизация последовательности для test
DO $$
DECLARE
    max_id BIGINT;
    seq_name TEXT;
BEGIN
    SELECT pg_get_serial_sequence('test', 'id') INTO seq_name;
    SELECT COALESCE(MAX(id), 0) FROM test INTO max_id;
    IF seq_name IS NOT NULL THEN
        EXECUTE format('SELECT setval(%L, %s, true)', seq_name, GREATEST(max_id, 1));
        RAISE NOTICE 'Synchronized sequence % to %', seq_name, GREATEST(max_id, 1);
    END IF;
END $$;

-- Синхронизация последовательности для exam_source
DO $$
DECLARE
    max_id BIGINT;
    seq_name TEXT;
BEGIN
    SELECT pg_get_serial_sequence('exam_source', 'id') INTO seq_name;
    SELECT COALESCE(MAX(id), 0) FROM exam_source INTO max_id;
    IF seq_name IS NOT NULL THEN
        EXECUTE format('SELECT setval(%L, %s, true)', seq_name, GREATEST(max_id, 1));
        RAISE NOTICE 'Synchronized sequence % to %', seq_name, GREATEST(max_id, 1);
    END IF;
END $$;

