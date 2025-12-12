-- Seed скрипт для демонстрации работы статистики
-- Создает темы, задания, тесты и попытки для пользователя с id=1 по предмету "Русский язык" (id=1)
-- 
-- ВАЖНО: 
-- 1. Перед выполнением этого скрипта убедитесь, что:
--    - Выполнены скрипты CalculateUserStatistics.sql и UpdateUserStatsOnAttemptComplete.sql
--    - В базе есть пользователь с id=1
--    - В базе есть предмет "Русский язык" с id=1
-- 
-- 2. После выполнения скрипта триггер автоматически обновит статистику в user_stats
--    при обновлении статуса попыток на 'completed'
--
-- 3. Скрипт создает:
--    - 5 тем по русскому языку
--    - 25 заданий (по 5 на каждую тему)
--    - 3 теста с разными комбинациями заданий
--    - 3 завершенные попытки с ответами пользователя

-- 1. Создаем темы для русского языка
INSERT INTO topic (subject_id, topic_name, topic_code, is_active)
VALUES
    (1, 'Орфография', 'ORF', TRUE),
    (1, 'Пунктуация', 'PUN', TRUE),
    (1, 'Морфология', 'MOR', TRUE),
    (1, 'Синтаксис', 'SIN', TRUE),
    (1, 'Лексика', 'LEX', TRUE)
ON CONFLICT DO NOTHING;

-- Получаем ID созданных тем
DO $$
DECLARE
    v_topic_orf BIGINT;
    v_topic_pun BIGINT;
    v_topic_mor BIGINT;
    v_topic_sin BIGINT;
    v_topic_lex BIGINT;
    v_test1_id BIGINT;
    v_test2_id BIGINT;
    v_test3_id BIGINT;
    v_attempt1_id BIGINT;
    v_attempt2_id BIGINT;
    v_attempt3_id BIGINT;
    v_started_at TIMESTAMPTZ;
BEGIN
    -- Получаем ID тем (если темы уже существуют, берем существующие)
    SELECT id INTO v_topic_orf FROM topic WHERE topic_code = 'ORF' AND subject_id = 1 LIMIT 1;
    SELECT id INTO v_topic_pun FROM topic WHERE topic_code = 'PUN' AND subject_id = 1 LIMIT 1;
    SELECT id INTO v_topic_mor FROM topic WHERE topic_code = 'MOR' AND subject_id = 1 LIMIT 1;
    SELECT id INTO v_topic_sin FROM topic WHERE topic_code = 'SIN' AND subject_id = 1 LIMIT 1;
    SELECT id INTO v_topic_lex FROM topic WHERE topic_code = 'LEX' AND subject_id = 1 LIMIT 1;

    -- 2. Создаем задания по темам
    -- Орфография (5 заданий)
    INSERT INTO task_item (subject_id, topic_id, task_type, difficulty, statement, correct_answer, explanation, is_active)
    VALUES
        (1, v_topic_orf, 'multiple_choice', 1, 'В каком слове пропущена буква О?', '{"value": "ворона"}'::jsonb, 'Правильно: ворона', TRUE),
        (1, v_topic_orf, 'multiple_choice', 2, 'В каком слове пропущена буква А?', '{"value": "корова"}'::jsonb, 'Правильно: корова', TRUE),
        (1, v_topic_orf, 'multiple_choice', 1, 'В каком слове пропущена буква Е?', '{"value": "берег"}'::jsonb, 'Правильно: берег', TRUE),
        (1, v_topic_orf, 'multiple_choice', 3, 'В каком слове пропущена буква И?', '{"value": "морской"}'::jsonb, 'Правильно: морской', TRUE),
        (1, v_topic_orf, 'multiple_choice', 2, 'В каком слове пропущена буква Я?', '{"value": "поляна"}'::jsonb, 'Правильно: поляна', TRUE),
    -- Пунктуация (5 заданий)
        (1, v_topic_pun, 'multiple_choice', 1, 'Где нужна запятая? "Он пришел(,) и сел."', '{"value": "пришел,"}'::jsonb, 'Нужна запятая перед союзом И', TRUE),
        (1, v_topic_pun, 'multiple_choice', 2, 'Где нужна запятая? "Я устал(,) поэтому отдохну."', '{"value": "устал,"}'::jsonb, 'Нужна запятая перед союзом поэтому', TRUE),
        (1, v_topic_pun, 'multiple_choice', 1, 'Где нужна запятая? "Она красивая(,) умная."', '{"value": "красивая,"}'::jsonb, 'Нужна запятая между однородными членами', TRUE),
        (1, v_topic_pun, 'multiple_choice', 3, 'Где нужна запятая? "Когда пришел(,) он сел."', '{"value": "пришел,"}'::jsonb, 'Нужна запятая после придаточного предложения', TRUE),
        (1, v_topic_pun, 'multiple_choice', 2, 'Где нужна запятая? "Он сказал(,) что придет."', '{"value": "сказал,"}'::jsonb, 'Нужна запятая перед союзом что', TRUE),
    -- Морфология (5 заданий)
        (1, v_topic_mor, 'multiple_choice', 1, 'Определите часть речи: "красивый"', '{"value": "прилагательное"}'::jsonb, 'Это прилагательное', TRUE),
        (1, v_topic_mor, 'multiple_choice', 2, 'Определите часть речи: "бегать"', '{"value": "глагол"}'::jsonb, 'Это глагол', TRUE),
        (1, v_topic_mor, 'multiple_choice', 1, 'Определите часть речи: "быстро"', '{"value": "наречие"}'::jsonb, 'Это наречие', TRUE),
        (1, v_topic_mor, 'multiple_choice', 3, 'Определите часть речи: "под"', '{"value": "предлог"}'::jsonb, 'Это предлог', TRUE),
        (1, v_topic_mor, 'multiple_choice', 2, 'Определите часть речи: "и"', '{"value": "союз"}'::jsonb, 'Это союз', TRUE),
    -- Синтаксис (5 заданий)
        (1, v_topic_sin, 'multiple_choice', 1, 'Найдите подлежащее: "Кот спит."', '{"value": "кот"}'::jsonb, 'Подлежащее - кот', TRUE),
        (1, v_topic_sin, 'multiple_choice', 2, 'Найдите сказуемое: "Он читает."', '{"value": "читает"}'::jsonb, 'Сказуемое - читает', TRUE),
        (1, v_topic_sin, 'multiple_choice', 1, 'Найдите дополнение: "Я вижу дом."', '{"value": "дом"}'::jsonb, 'Дополнение - дом', TRUE),
        (1, v_topic_sin, 'multiple_choice', 3, 'Найдите определение: "Красивый дом."', '{"value": "красивый"}'::jsonb, 'Определение - красивый', TRUE),
        (1, v_topic_sin, 'multiple_choice', 2, 'Найдите обстоятельство: "Он пришел быстро."', '{"value": "быстро"}'::jsonb, 'Обстоятельство - быстро', TRUE),
    -- Лексика (5 заданий)
        (1, v_topic_lex, 'multiple_choice', 1, 'Найдите синоним к слову "большой"', '{"value": "огромный"}'::jsonb, 'Синоним - огромный', TRUE),
        (1, v_topic_lex, 'multiple_choice', 2, 'Найдите антоним к слову "день"', '{"value": "ночь"}'::jsonb, 'Антоним - ночь', TRUE),
        (1, v_topic_lex, 'multiple_choice', 1, 'Найдите синоним к слову "красивый"', '{"value": "прекрасный"}'::jsonb, 'Синоним - прекрасный', TRUE),
        (1, v_topic_lex, 'multiple_choice', 3, 'Найдите антоним к слову "хороший"', '{"value": "плохой"}'::jsonb, 'Антоним - плохой', TRUE),
        (1, v_topic_lex, 'multiple_choice', 2, 'Найдите синоним к слову "умный"', '{"value": "разумный"}'::jsonb, 'Синоним - разумный', TRUE)
    ON CONFLICT DO NOTHING;

    -- 3. Создаем тесты
    INSERT INTO test (subject_id, test_kind, title, author_id, time_limit_sec, is_published, is_public, is_state_archive)
    VALUES
        (1, 'practice', 'Тест по орфографии и пунктуации', 1, 1800, TRUE, TRUE, FALSE),
        (1, 'practice', 'Тест по морфологии и синтаксису', 1, 1800, TRUE, TRUE, FALSE),
        (1, 'practice', 'Комплексный тест по русскому языку', 1, 3600, TRUE, TRUE, FALSE)
    ON CONFLICT DO NOTHING;

    -- Получаем ID тестов (если тесты уже существуют, берем существующие)
    SELECT id INTO v_test1_id FROM test WHERE title = 'Тест по орфографии и пунктуации' AND subject_id = 1 LIMIT 1;
    SELECT id INTO v_test2_id FROM test WHERE title = 'Тест по морфологии и синтаксису' AND subject_id = 1 LIMIT 1;
    SELECT id INTO v_test3_id FROM test WHERE title = 'Комплексный тест по русскому языку' AND subject_id = 1 LIMIT 1;

    -- 4. Добавляем задания в тесты
    -- Тест 1: Орфография (первые 3) + Пунктуация (первые 3)
    INSERT INTO test_task (test_id, task_id, position)
    SELECT v_test1_id, id, ROW_NUMBER() OVER (ORDER BY topic_id, id)
    FROM task_item
    WHERE subject_id = 1
      AND (
          (topic_id = v_topic_orf AND id IN (
              SELECT id FROM task_item WHERE topic_id = v_topic_orf ORDER BY id LIMIT 3
          ))
          OR
          (topic_id = v_topic_pun AND id IN (
              SELECT id FROM task_item WHERE topic_id = v_topic_pun ORDER BY id LIMIT 3
          ))
      )
    ON CONFLICT DO NOTHING;

    -- Тест 2: Морфология (первые 3) + Синтаксис (первые 3)
    INSERT INTO test_task (test_id, task_id, position)
    SELECT v_test2_id, id, ROW_NUMBER() OVER (ORDER BY topic_id, id)
    FROM task_item
    WHERE subject_id = 1
      AND (
          (topic_id = v_topic_mor AND id IN (
              SELECT id FROM task_item WHERE topic_id = v_topic_mor ORDER BY id LIMIT 3
          ))
          OR
          (topic_id = v_topic_sin AND id IN (
              SELECT id FROM task_item WHERE topic_id = v_topic_sin ORDER BY id LIMIT 3
          ))
      )
    ON CONFLICT DO NOTHING;

    -- Тест 3: Все темы по 2 задания
    INSERT INTO test_task (test_id, task_id, position)
    SELECT v_test3_id, id, ROW_NUMBER() OVER (ORDER BY topic_id, id)
    FROM task_item
    WHERE subject_id = 1
      AND (
          (topic_id = v_topic_orf AND id IN (SELECT id FROM task_item WHERE topic_id = v_topic_orf ORDER BY id LIMIT 2))
          OR (topic_id = v_topic_pun AND id IN (SELECT id FROM task_item WHERE topic_id = v_topic_pun ORDER BY id LIMIT 2))
          OR (topic_id = v_topic_mor AND id IN (SELECT id FROM task_item WHERE topic_id = v_topic_mor ORDER BY id LIMIT 2))
          OR (topic_id = v_topic_sin AND id IN (SELECT id FROM task_item WHERE topic_id = v_topic_sin ORDER BY id LIMIT 2))
          OR (topic_id = v_topic_lex AND id IN (SELECT id FROM task_item WHERE topic_id = v_topic_lex ORDER BY id LIMIT 2))
      )
    ON CONFLICT DO NOTHING;

    -- 5. Создаем попытки для пользователя с id=1
    -- Сначала создаем со статусом 'in_progress', затем обновим на 'completed' для срабатывания триггера
    v_started_at := NOW() - INTERVAL '3 days';
    
    -- Попытка 1: Тест 1 - будет завершена, 4 из 6 правильных (66%)
    INSERT INTO attempt (test_id, user_id, started_at, finished_at, status, raw_score, scaled_score, duration_sec, seed)
    VALUES (v_test1_id, 1, v_started_at, v_started_at + INTERVAL '25 minutes', 'in_progress', 66.67, 66.67, 1500, 12345)
    RETURNING id INTO v_attempt1_id;

    -- Попытка 2: Тест 2 - будет завершена, 5 из 6 правильных (83%)
    v_started_at := NOW() - INTERVAL '2 days';
    INSERT INTO attempt (test_id, user_id, started_at, finished_at, status, raw_score, scaled_score, duration_sec, seed)
    VALUES (v_test2_id, 1, v_started_at, v_started_at + INTERVAL '20 minutes', 'in_progress', 83.33, 83.33, 1200, 23456)
    RETURNING id INTO v_attempt2_id;

    -- Попытка 3: Тест 3 - будет завершена, 7 из 10 правильных (70%)
    v_started_at := NOW() - INTERVAL '1 day';
    INSERT INTO attempt (test_id, user_id, started_at, finished_at, status, raw_score, scaled_score, duration_sec, seed)
    VALUES (v_test3_id, 1, v_started_at, v_started_at + INTERVAL '35 minutes', 'in_progress', 70.00, 70.00, 2100, 34567)
    RETURNING id INTO v_attempt3_id;

    -- 6. Создаем ответы пользователя
    -- Попытка 1: Тест 1 (орфография + пунктуация)
    -- Орфография: 2 правильных из 3, Пунктуация: 2 правильных из 3
    INSERT INTO user_answer (attempt_id, task_id, given_answer, is_correct, time_spent_sec)
    SELECT v_attempt1_id, tt.task_id, 
           CASE 
               WHEN ROW_NUMBER() OVER (ORDER BY tt.position) <= 4 THEN ti.correct_answer
               ELSE '{"value": "неправильный ответ"}'::jsonb
           END,
           ROW_NUMBER() OVER (ORDER BY tt.position) <= 4,
           250
    FROM test_task tt
    JOIN task_item ti ON ti.id = tt.task_id
    WHERE tt.test_id = v_test1_id
    ORDER BY tt.position;

    -- Попытка 2: Тест 2 (морфология + синтаксис)
    -- Морфология: 3 правильных из 3, Синтаксис: 2 правильных из 3
    INSERT INTO user_answer (attempt_id, task_id, given_answer, is_correct, time_spent_sec)
    SELECT v_attempt2_id, tt.task_id,
           CASE 
               WHEN ROW_NUMBER() OVER (ORDER BY tt.position) <= 5 THEN ti.correct_answer
               ELSE '{"value": "неправильный ответ"}'::jsonb
           END,
           ROW_NUMBER() OVER (ORDER BY tt.position) <= 5,
           200
    FROM test_task tt
    JOIN task_item ti ON ti.id = tt.task_id
    WHERE tt.test_id = v_test2_id
    ORDER BY tt.position;

    -- Попытка 3: Тест 3 (все темы)
    -- Орфография: 1 из 2, Пунктуация: 2 из 2, Морфология: 1 из 2, Синтаксис: 2 из 2, Лексика: 1 из 2
    INSERT INTO user_answer (attempt_id, task_id, given_answer, is_correct, time_spent_sec)
    SELECT v_attempt3_id, tt.task_id,
           CASE 
               -- Орфография (первые 2): 1 правильный
               WHEN tt.position = 1 THEN ti.correct_answer
               WHEN tt.position = 2 THEN '{"value": "неправильный"}'::jsonb
               -- Пунктуация (3-4): оба правильные
               WHEN tt.position BETWEEN 3 AND 4 THEN ti.correct_answer
               -- Морфология (5-6): 1 правильный
               WHEN tt.position = 5 THEN ti.correct_answer
               WHEN tt.position = 6 THEN '{"value": "неправильный"}'::jsonb
               -- Синтаксис (7-8): оба правильные
               WHEN tt.position BETWEEN 7 AND 8 THEN ti.correct_answer
               -- Лексика (9-10): 1 правильный
               WHEN tt.position = 9 THEN ti.correct_answer
               WHEN tt.position = 10 THEN '{"value": "неправильный"}'::jsonb
               ELSE '{"value": "неправильный"}'::jsonb
           END,
           CASE 
               WHEN tt.position IN (1, 3, 4, 5, 7, 8, 9) THEN TRUE
               ELSE FALSE
           END,
           210
    FROM test_task tt
    JOIN task_item ti ON ti.id = tt.task_id
    WHERE tt.test_id = v_test3_id
    ORDER BY tt.position;

    -- 7. Обновляем статус попыток, чтобы триггер сработал и обновил статистику
    -- Триггер срабатывает при UPDATE статуса на 'completed'
    -- Сначала создаем попытки со статусом 'in_progress', затем обновляем на 'completed'
    UPDATE attempt SET status = 'in_progress' WHERE id IN (v_attempt1_id, v_attempt2_id, v_attempt3_id);
    UPDATE attempt SET status = 'completed' WHERE id = v_attempt1_id;
    UPDATE attempt SET status = 'completed' WHERE id = v_attempt2_id;
    UPDATE attempt SET status = 'completed' WHERE id = v_attempt3_id;

END $$;

