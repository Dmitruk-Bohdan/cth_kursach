SELECT
    id,
    teacher_id,
    student_id,
    status,
    established_at,
    created_at,
    updated_at
FROM teacher_student
WHERE teacher_id = @teacher_id
  AND student_id = @student_id;

