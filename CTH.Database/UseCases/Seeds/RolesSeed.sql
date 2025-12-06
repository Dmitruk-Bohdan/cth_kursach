INSERT INTO role (id, role_name, description)
VALUES
    (1, 'student', 'Ученик системы'),
    (2, 'teacher', 'Преподаватель'),
    (3, 'admin', 'Администратор')
ON CONFLICT (role_name) DO NOTHING;
