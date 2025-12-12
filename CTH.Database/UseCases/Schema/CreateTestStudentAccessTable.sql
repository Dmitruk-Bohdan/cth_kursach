-- Таблица для хранения доступа студентов к приватным тестам
CREATE TABLE IF NOT EXISTS test_student_access
(
    id         BIGSERIAL PRIMARY KEY,
    test_id    BIGINT      NOT NULL REFERENCES test (id) ON DELETE CASCADE,
    student_id BIGINT      NOT NULL REFERENCES user_account (id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (test_id, student_id)
);

CREATE INDEX IF NOT EXISTS idx_test_student_access_test_id ON test_student_access (test_id);
CREATE INDEX IF NOT EXISTS idx_test_student_access_student_id ON test_student_access (student_id);

