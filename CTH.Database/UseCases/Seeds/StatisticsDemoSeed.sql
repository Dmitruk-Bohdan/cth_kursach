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
--
-- 4. Скрипт ИДЕМПОТЕНТЕН - его можно выполнять многократно:
--    - Старые попытки и ответы с seed (12345, 23456, 34567) будут удалены перед созданием новых
--    - Старая статистика по предмету будет очищена перед пересчетом
--    - Старые темы, задания и тесты будут удалены перед созданием новых

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
    -- Удаляем старые данные в правильном порядке (из-за внешних ключей)
    -- 1. Удаляем ответы пользователей для всех попыток, которые ссылаются на тесты с этими названиями
    DELETE FROM user_answer WHERE attempt_id IN (
        SELECT a.id FROM attempt a
        JOIN test t ON t.id = a.test_id
        WHERE t.subject_id = 1 
          AND t.title IN (
              'Тест по орфографии и пунктуации',
              'Тест по морфологии и синтаксису',
              'Комплексный тест по русскому языку'
          )
    );
    
    -- 2. Удаляем ВСЕ попытки, которые ссылаются на эти тесты
    DELETE FROM attempt 
    WHERE test_id IN (
        SELECT id FROM test 
        WHERE subject_id = 1 
          AND title IN (
              'Тест по орфографии и пунктуации',
              'Тест по морфологии и синтаксису',
              'Комплексный тест по русскому языку'
          )
    );
    
    -- 3. Удаляем связи тестов с заданиями
    DELETE FROM test_task 
    WHERE test_id IN (
        SELECT id FROM test 
        WHERE subject_id = 1 
          AND title IN (
              'Тест по орфографии и пунктуации',
              'Тест по морфологии и синтаксису',
              'Комплексный тест по русскому языку'
          )
    );
    
    -- 4. Удаляем тесты
    DELETE FROM test 
    WHERE subject_id = 1 
      AND title IN (
          'Тест по орфографии и пунктуации',
          'Тест по морфологии и синтаксису',
          'Комплексный тест по русскому языку'
      );
    
    -- 5. Удаляем статистику по предмету (ПЕРЕД удалением тем, т.к. user_stats ссылается на topic)
    DELETE FROM user_stats WHERE user_id = 1 AND subject_id = 1;
    
    -- 6. Удаляем задания, которые ссылаются на темы
    DELETE FROM task_item WHERE subject_id = 1 AND topic_id IN (
        SELECT id FROM topic WHERE subject_id = 1 AND topic_code IN ('ORF', 'PUN', 'MOR', 'SIN', 'LEX')
    );
    
    -- 7. Удаляем старые темы
    DELETE FROM topic WHERE subject_id = 1 AND topic_code IN ('ORF', 'PUN', 'MOR', 'SIN', 'LEX');

    -- 1. Создаем темы для русского языка
    INSERT INTO topic (subject_id, topic_name, topic_code, is_active)
    VALUES
        (1, 'Орфография', 'ORF', TRUE),
        (1, 'Пунктуация', 'PUN', TRUE),
        (1, 'Морфология', 'MOR', TRUE),
        (1, 'Синтаксис', 'SIN', TRUE),
        (1, 'Лексика', 'LEX', TRUE);

    -- Получаем ID тем (если темы уже существуют, берем существующие)
    SELECT id INTO v_topic_orf FROM topic WHERE topic_code = 'ORF' AND subject_id = 1 LIMIT 1;
    SELECT id INTO v_topic_pun FROM topic WHERE topic_code = 'PUN' AND subject_id = 1 LIMIT 1;
    SELECT id INTO v_topic_mor FROM topic WHERE topic_code = 'MOR' AND subject_id = 1 LIMIT 1;
    SELECT id INTO v_topic_sin FROM topic WHERE topic_code = 'SIN' AND subject_id = 1 LIMIT 1;
    SELECT id INTO v_topic_lex FROM topic WHERE topic_code = 'LEX' AND subject_id = 1 LIMIT 1;

    -- 2. Создаем задания по темам
    -- Орфография (5 заданий)
    -- Для multiple_choice правильный ответ - это номер варианта (1, 2, 3, 4)
    INSERT INTO task_item (subject_id, topic_id, task_type, difficulty, statement, correct_answer, explanation, is_active)
    VALUES
        (1, v_topic_orf, 'multiple_choice', 1, 'В каком слове пропущена буква О? Варианты: 1) ворона 2) варана 3) варона 4) варана', '{"value": "1", "options": ["ворона", "варана", "варона", "варана"]}'::jsonb, 'Правильно: ворона', TRUE),
        (1, v_topic_orf, 'multiple_choice', 2, 'В каком слове пропущена буква А? Варианты: 1) карова 2) корова 3) карова 4) корова', '{"value": "2", "options": ["карова", "корова", "карова", "корова"]}'::jsonb, 'Правильно: корова', TRUE),
        (1, v_topic_orf, 'multiple_choice', 1, 'В каком слове пропущена буква Е? Варианты: 1) берег 2) барег 3) бариг 4) берег', '{"value": "1", "options": ["берег", "барег", "бариг", "берег"]}'::jsonb, 'Правильно: берег', TRUE),
        (1, v_topic_orf, 'multiple_choice', 3, 'В каком слове пропущена буква И? Варианты: 1) марской 2) морской 3) марской 4) морской', '{"value": "2", "options": ["марской", "морской", "марской", "морской"]}'::jsonb, 'Правильно: морской', TRUE),
        (1, v_topic_orf, 'multiple_choice', 2, 'В каком слове пропущена буква Я? Варианты: 1) поляна 2) полина 3) поляна 4) полина', '{"value": "1", "options": ["поляна", "полина", "поляна", "полина"]}'::jsonb, 'Правильно: поляна', TRUE),
    -- Пунктуация (5 заданий)
        (1, v_topic_pun, 'multiple_choice', 1, 'Где нужна запятая? "Он пришел(,) и сел." Варианты: 1) пришел, 2) пришел 3) сел, 4) сел', '{"value": "1", "options": ["пришел,", "пришел", "сел,", "сел"]}'::jsonb, 'Нужна запятая перед союзом И', TRUE),
        (1, v_topic_pun, 'multiple_choice', 2, 'Где нужна запятая? "Я устал(,) поэтому отдохну." Варианты: 1) устал, 2) устал 3) поэтому, 4) отдохну', '{"value": "1", "options": ["устал,", "устал", "поэтому,", "отдохну"]}'::jsonb, 'Нужна запятая перед союзом поэтому', TRUE),
        (1, v_topic_pun, 'multiple_choice', 1, 'Где нужна запятая? "Она красивая(,) умная." Варианты: 1) красивая, 2) красивая 3) умная, 4) умная', '{"value": "1", "options": ["красивая,", "красивая", "умная,", "умная"]}'::jsonb, 'Нужна запятая между однородными членами', TRUE),
        (1, v_topic_pun, 'multiple_choice', 3, 'Где нужна запятая? "Когда пришел(,) он сел." Варианты: 1) пришел, 2) пришел 3) сел, 4) сел', '{"value": "1", "options": ["пришел,", "пришел", "сел,", "сел"]}'::jsonb, 'Нужна запятая после придаточного предложения', TRUE),
        (1, v_topic_pun, 'multiple_choice', 2, 'Где нужна запятая? "Он сказал(,) что придет." Варианты: 1) сказал, 2) сказал 3) что, 4) придет', '{"value": "1", "options": ["сказал,", "сказал", "что,", "придет"]}'::jsonb, 'Нужна запятая перед союзом что', TRUE),
    -- Морфология (5 заданий)
        (1, v_topic_mor, 'multiple_choice', 1, 'Определите часть речи: "красивый". Варианты: 1) существительное 2) прилагательное 3) глагол 4) наречие', '{"value": "2", "options": ["существительное", "прилагательное", "глагол", "наречие"]}'::jsonb, 'Это прилагательное', TRUE),
        (1, v_topic_mor, 'multiple_choice', 2, 'Определите часть речи: "бегать". Варианты: 1) существительное 2) прилагательное 3) глагол 4) наречие', '{"value": "3", "options": ["существительное", "прилагательное", "глагол", "наречие"]}'::jsonb, 'Это глагол', TRUE),
        (1, v_topic_mor, 'multiple_choice', 1, 'Определите часть речи: "быстро". Варианты: 1) существительное 2) прилагательное 3) глагол 4) наречие', '{"value": "4", "options": ["существительное", "прилагательное", "глагол", "наречие"]}'::jsonb, 'Это наречие', TRUE),
        (1, v_topic_mor, 'multiple_choice', 3, 'Определите часть речи: "под". Варианты: 1) предлог 2) союз 3) частица 4) междометие', '{"value": "1", "options": ["предлог", "союз", "частица", "междометие"]}'::jsonb, 'Это предлог', TRUE),
        (1, v_topic_mor, 'multiple_choice', 2, 'Определите часть речи: "и". Варианты: 1) предлог 2) союз 3) частица 4) междометие', '{"value": "2", "options": ["предлог", "союз", "частица", "междометие"]}'::jsonb, 'Это союз', TRUE),
    -- Синтаксис (5 заданий)
        (1, v_topic_sin, 'multiple_choice', 1, 'Найдите подлежащее: "Кот спит." Варианты: 1) кот 2) спит 3) кот спит 4) нет подлежащего', '{"value": "1", "options": ["кот", "спит", "кот спит", "нет подлежащего"]}'::jsonb, 'Подлежащее - кот', TRUE),
        (1, v_topic_sin, 'multiple_choice', 2, 'Найдите сказуемое: "Он читает." Варианты: 1) он 2) читает 3) он читает 4) нет сказуемого', '{"value": "2", "options": ["он", "читает", "он читает", "нет сказуемого"]}'::jsonb, 'Сказуемое - читает', TRUE),
        (1, v_topic_sin, 'multiple_choice', 1, 'Найдите дополнение: "Я вижу дом." Варианты: 1) я 2) вижу 3) дом 4) нет дополнения', '{"value": "3", "options": ["я", "вижу", "дом", "нет дополнения"]}'::jsonb, 'Дополнение - дом', TRUE),
        (1, v_topic_sin, 'multiple_choice', 3, 'Найдите определение: "Красивый дом." Варианты: 1) красивый 2) дом 3) красивый дом 4) нет определения', '{"value": "1", "options": ["красивый", "дом", "красивый дом", "нет определения"]}'::jsonb, 'Определение - красивый', TRUE),
        (1, v_topic_sin, 'multiple_choice', 2, 'Найдите обстоятельство: "Он пришел быстро." Варианты: 1) он 2) пришел 3) быстро 4) нет обстоятельства', '{"value": "3", "options": ["он", "пришел", "быстро", "нет обстоятельства"]}'::jsonb, 'Обстоятельство - быстро', TRUE),
    -- Лексика (5 заданий)
        (1, v_topic_lex, 'multiple_choice', 1, 'Найдите синоним к слову "большой". Варианты: 1) маленький 2) огромный 3) средний 4) крошечный', '{"value": "2", "options": ["маленький", "огромный", "средний", "крошечный"]}'::jsonb, 'Синоним - огромный', TRUE),
        (1, v_topic_lex, 'multiple_choice', 2, 'Найдите антоним к слову "день". Варианты: 1) утро 2) вечер 3) ночь 4) полдень', '{"value": "3", "options": ["утро", "вечер", "ночь", "полдень"]}'::jsonb, 'Антоним - ночь', TRUE),
        (1, v_topic_lex, 'multiple_choice', 1, 'Найдите синоним к слову "красивый". Варианты: 1) уродливый 2) прекрасный 3) обычный 4) странный', '{"value": "2", "options": ["уродливый", "прекрасный", "обычный", "странный"]}'::jsonb, 'Синоним - прекрасный', TRUE),
        (1, v_topic_lex, 'multiple_choice', 3, 'Найдите антоним к слову "хороший". Варианты: 1) отличный 2) плохой 3) нормальный 4) замечательный', '{"value": "2", "options": ["отличный", "плохой", "нормальный", "замечательный"]}'::jsonb, 'Антоним - плохой', TRUE),
        (1, v_topic_lex, 'multiple_choice', 2, 'Найдите синоним к слову "умный". Варианты: 1) глупый 2) разумный 3) обычный 4) простой', '{"value": "2", "options": ["глупый", "разумный", "обычный", "простой"]}'::jsonb, 'Синоним - разумный', TRUE)
    ON CONFLICT DO NOTHING;

    -- 3. Создаем тесты (государственные)
    -- Сначала получаем ID существующих тестов (если есть)
    SELECT id INTO v_test1_id FROM test WHERE title = 'Тест по орфографии и пунктуации' AND subject_id = 1 LIMIT 1;
    SELECT id INTO v_test2_id FROM test WHERE title = 'Тест по морфологии и синтаксису' AND subject_id = 1 LIMIT 1;
    SELECT id INTO v_test3_id FROM test WHERE title = 'Комплексный тест по русскому языку' AND subject_id = 1 LIMIT 1;
    
    -- Если тесты существуют, удаляем их (уже удалили попытки выше)
    IF v_test1_id IS NOT NULL THEN
        DELETE FROM test_task WHERE test_id = v_test1_id;
        DELETE FROM test WHERE id = v_test1_id;
    END IF;
    IF v_test2_id IS NOT NULL THEN
        DELETE FROM test_task WHERE test_id = v_test2_id;
        DELETE FROM test WHERE id = v_test2_id;
    END IF;
    IF v_test3_id IS NOT NULL THEN
        DELETE FROM test_task WHERE test_id = v_test3_id;
        DELETE FROM test WHERE id = v_test3_id;
    END IF;
    
    -- Создаем новые тесты
    INSERT INTO test (subject_id, test_kind, title, author_id, time_limit_sec, is_published, is_public, is_state_archive)
    VALUES
        (1, 'PAST_EXAM', 'Тест по орфографии и пунктуации', NULL, 1800, TRUE, TRUE, TRUE),
        (1, 'PAST_EXAM', 'Тест по морфологии и синтаксису', NULL, 1800, TRUE, TRUE, TRUE),
        (1, 'PAST_EXAM', 'Комплексный тест по русскому языку', NULL, 3600, TRUE, TRUE, TRUE);

    -- Получаем ID созданных тестов
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

    -- 5. Удаляем старые попытки и ответы для демонстрации (если скрипт выполняется повторно)
    DELETE FROM user_answer WHERE attempt_id IN (
        SELECT id FROM attempt WHERE user_id = 1 AND test_id IN (v_test1_id, v_test2_id, v_test3_id) AND seed IN (12345, 23456, 34567)
    );
    DELETE FROM attempt WHERE user_id = 1 AND test_id IN (v_test1_id, v_test2_id, v_test3_id) AND seed IN (12345, 23456, 34567);
    
    -- Создаем попытки для пользователя с id=1
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
    -- Для multiple_choice ответ должен быть номером варианта (цифра: "1", "2", "3", "4")
    -- Попытка 1: Тест 1 (орфография + пунктуация)
    -- Орфография: 2 правильных из 3, Пунктуация: 2 правильных из 3
    INSERT INTO user_answer (attempt_id, task_id, given_answer, is_correct, time_spent_sec)
    SELECT v_attempt1_id, tt.task_id, 
           CASE 
               WHEN ROW_NUMBER() OVER (ORDER BY tt.position) <= 4 THEN 
                   -- Правильный ответ - берем value из correct_answer
                   jsonb_build_object('value', ti.correct_answer->>'value')
               ELSE 
                   -- Неправильный ответ - выбираем другой вариант (2, 3 или 4)
                   jsonb_build_object('value', CASE 
                       WHEN ti.correct_answer->>'value' = '1' THEN '2'
                       WHEN ti.correct_answer->>'value' = '2' THEN '3'
                       WHEN ti.correct_answer->>'value' = '3' THEN '4'
                       ELSE '1'
                   END)
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
               WHEN ROW_NUMBER() OVER (ORDER BY tt.position) <= 5 THEN 
                   jsonb_build_object('value', ti.correct_answer->>'value')
               ELSE 
                   jsonb_build_object('value', CASE 
                       WHEN ti.correct_answer->>'value' = '1' THEN '2'
                       WHEN ti.correct_answer->>'value' = '2' THEN '3'
                       WHEN ti.correct_answer->>'value' = '3' THEN '4'
                       ELSE '1'
                   END)
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
               WHEN tt.position = 1 THEN jsonb_build_object('value', ti.correct_answer->>'value')
               WHEN tt.position = 2 THEN jsonb_build_object('value', CASE 
                   WHEN ti.correct_answer->>'value' = '1' THEN '2'
                   WHEN ti.correct_answer->>'value' = '2' THEN '3'
                   WHEN ti.correct_answer->>'value' = '3' THEN '4'
                   ELSE '1'
               END)
               -- Пунктуация (3-4): оба правильные
               WHEN tt.position BETWEEN 3 AND 4 THEN jsonb_build_object('value', ti.correct_answer->>'value')
               -- Морфология (5-6): 1 правильный
               WHEN tt.position = 5 THEN jsonb_build_object('value', ti.correct_answer->>'value')
               WHEN tt.position = 6 THEN jsonb_build_object('value', CASE 
                   WHEN ti.correct_answer->>'value' = '1' THEN '2'
                   WHEN ti.correct_answer->>'value' = '2' THEN '3'
                   WHEN ti.correct_answer->>'value' = '3' THEN '4'
                   ELSE '1'
               END)
               -- Синтаксис (7-8): оба правильные
               WHEN tt.position BETWEEN 7 AND 8 THEN jsonb_build_object('value', ti.correct_answer->>'value')
               -- Лексика (9-10): 1 правильный
               WHEN tt.position = 9 THEN jsonb_build_object('value', ti.correct_answer->>'value')
               WHEN tt.position = 10 THEN jsonb_build_object('value', CASE 
                   WHEN ti.correct_answer->>'value' = '1' THEN '2'
                   WHEN ti.correct_answer->>'value' = '2' THEN '3'
                   WHEN ti.correct_answer->>'value' = '3' THEN '4'
                   ELSE '1'
               END)
               ELSE jsonb_build_object('value', '1')
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

    -- 7. Очищаем старую статистику для пользователя по этому предмету (если скрипт выполняется повторно)
    DELETE FROM user_stats WHERE user_id = 1 AND subject_id = 1;
    
    -- 8. Обновляем статус попыток, чтобы триггер сработал и обновил статистику
    -- Триггер срабатывает при UPDATE статуса на 'completed'
    -- Сначала создаем попытки со статусом 'in_progress', затем обновляем на 'completed'
    UPDATE attempt SET status = 'in_progress' WHERE id IN (v_attempt1_id, v_attempt2_id, v_attempt3_id);
    UPDATE attempt SET status = 'completed' WHERE id = v_attempt1_id;
    UPDATE attempt SET status = 'completed' WHERE id = v_attempt2_id;
    UPDATE attempt SET status = 'completed' WHERE id = v_attempt3_id;

END $$;

