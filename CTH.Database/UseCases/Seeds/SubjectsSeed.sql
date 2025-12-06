INSERT INTO subject (id, subject_code, subject_name)
VALUES
    (1, 'RUS', 'Русский язык')
ON CONFLICT (subject_code) DO NOTHING;
