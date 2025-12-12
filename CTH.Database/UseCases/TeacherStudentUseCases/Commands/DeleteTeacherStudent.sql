DELETE FROM teacher_student
WHERE teacher_id = @teacher_id
  AND student_id = @student_id;

