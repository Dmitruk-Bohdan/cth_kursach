-- Base reference tables
CREATE TABLE IF NOT EXISTS role
(
    id          BIGSERIAL PRIMARY KEY,
    role_name   VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS user_account
(
    id            BIGSERIAL PRIMARY KEY,
    user_name     VARCHAR(150) NOT NULL,
    email         VARCHAR(254) NOT NULL UNIQUE,
    password_hash VARCHAR(256) NOT NULL,
    role_type_id  BIGINT       NOT NULL REFERENCES role (id),
    last_login_at TIMESTAMPTZ,
    created_at    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at    TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS subject
(
    id           BIGSERIAL PRIMARY KEY,
    subject_code VARCHAR(32)  NOT NULL UNIQUE,
    subject_name VARCHAR(150) NOT NULL,
    is_active    BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at   TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS topic
(
    id             BIGSERIAL PRIMARY KEY,
    subject_id     BIGINT       NOT NULL REFERENCES subject (id),
    topic_name     VARCHAR(150) NOT NULL,
    topic_code     VARCHAR(64),
    topic_parent_id BIGINT REFERENCES topic (id),
    is_active      BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at     TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at     TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS exam_source
(
    id         BIGSERIAL PRIMARY KEY,
    year       INTEGER       NOT NULL,
    variant_number INTEGER,
    issuer     VARCHAR(150),
    notes      TEXT,
    created_at TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

-- Task/test related tables
CREATE TABLE IF NOT EXISTS task_item
(
    id            BIGSERIAL PRIMARY KEY,
    subject_id    BIGINT      NOT NULL REFERENCES subject (id),
    topic_id      BIGINT REFERENCES topic (id),
    exam_source_id BIGINT REFERENCES exam_source (id),
    task_type     VARCHAR(64) NOT NULL,
    difficulty    SMALLINT    NOT NULL,
    statement     TEXT        NOT NULL,
    correct_answer JSONB      NOT NULL,
    explanation   TEXT,
    is_active     BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS test
(
    id              BIGSERIAL PRIMARY KEY,
    subject_id      BIGINT       NOT NULL REFERENCES subject (id),
    test_kind       VARCHAR(64)  NOT NULL,
    title           VARCHAR(200) NOT NULL,
    author_id       BIGINT REFERENCES user_account (id),
    time_limit_sec  INTEGER,
    attempts_allowed SMALLINT,
    mode            VARCHAR(50),
    is_published    BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS test_task
(
    id         BIGSERIAL PRIMARY KEY,
    test_id    BIGINT       NOT NULL REFERENCES test (id) ON DELETE CASCADE,
    task_id    BIGINT       NOT NULL REFERENCES task_item (id),
    position   INTEGER      NOT NULL,
    weight     NUMERIC(6,2),
    created_at TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    UNIQUE (test_id, task_id),
    UNIQUE (test_id, position)
);

CREATE TABLE IF NOT EXISTS teacher_student
(
    id            BIGSERIAL PRIMARY KEY,
    teacher_id    BIGINT      NOT NULL REFERENCES user_account (id),
    student_id    BIGINT      NOT NULL REFERENCES user_account (id),
    status        VARCHAR(50) NOT NULL,
    established_at TIMESTAMPTZ,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (teacher_id, student_id)
);

CREATE TABLE IF NOT EXISTS assignment
(
    id               BIGSERIAL PRIMARY KEY,
    test_id          BIGINT      NOT NULL REFERENCES test (id),
    teacher_id       BIGINT      NOT NULL REFERENCES user_account (id),
    student_id       BIGINT      NOT NULL REFERENCES user_account (id),
    due_at           TIMESTAMPTZ,
    attempts_allowed SMALLINT,
    status           VARCHAR(50) NOT NULL,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS attempt
(
    id           BIGSERIAL PRIMARY KEY,
    test_id      BIGINT      NOT NULL REFERENCES test (id),
    user_id      BIGINT      NOT NULL REFERENCES user_account (id),
    assignment_id BIGINT REFERENCES assignment (id),
    started_at   TIMESTAMPTZ NOT NULL,
    finished_at  TIMESTAMPTZ,
    status       VARCHAR(50) NOT NULL,
    raw_score    NUMERIC(6,2),
    scaled_score NUMERIC(6,2),
    duration_sec INTEGER,
    seed         BIGINT,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS user_answer
(
    id            BIGSERIAL PRIMARY KEY,
    attempt_id    BIGINT      NOT NULL REFERENCES attempt (id) ON DELETE CASCADE,
    task_id       BIGINT      NOT NULL REFERENCES task_item (id),
    given_answer  JSONB       NOT NULL,
    is_correct    BOOLEAN     NOT NULL,
    time_spent_sec INTEGER,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS user_stats
(
    id              BIGSERIAL PRIMARY KEY,
    user_id         BIGINT      NOT NULL REFERENCES user_account (id),
    subject_id      BIGINT REFERENCES subject (id),
    topic_id        BIGINT REFERENCES topic (id),
    attempts_total  INTEGER     NOT NULL DEFAULT 0,
    correct_total   INTEGER     NOT NULL DEFAULT 0,
    last_attempt_at TIMESTAMPTZ,
    average_score   NUMERIC(6,2),
    average_time_sec INTEGER,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS recommendation
(
    id          BIGSERIAL PRIMARY KEY,
    user_id     BIGINT      NOT NULL REFERENCES user_account (id),
    subject_id  BIGINT      NOT NULL REFERENCES subject (id),
    topic_id    BIGINT      NOT NULL REFERENCES topic (id),
    priority    SMALLINT    NOT NULL DEFAULT 1,
    reason_code VARCHAR(64) NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS notification
(
    id         BIGSERIAL PRIMARY KEY,
    user_id    BIGINT      NOT NULL REFERENCES user_account (id),
    type       VARCHAR(64) NOT NULL,
    payload    JSONB,
    status     VARCHAR(32) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS audit_log
(
    id          BIGSERIAL PRIMARY KEY,
    user_id     BIGINT REFERENCES user_account (id),
    action      VARCHAR(64) NOT NULL,
    entity_name VARCHAR(100) NOT NULL,
    entity_id   BIGINT,
    description TEXT,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS user_sessions
(
    id         BIGSERIAL PRIMARY KEY,
    user_id    BIGINT      NOT NULL REFERENCES user_account (id) ON DELETE CASCADE,
    jti        UUID        NOT NULL UNIQUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ NOT NULL,
    revoked_at TIMESTAMPTZ
);
