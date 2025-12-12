INSERT INTO teacher_student (teacher_id, student_id, status, established_at)
VALUES (@teacher_id, @student_id, @status, @established_at)
RETURNING id;

