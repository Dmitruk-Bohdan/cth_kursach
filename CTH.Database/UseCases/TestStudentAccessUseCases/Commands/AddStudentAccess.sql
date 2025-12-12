INSERT INTO test_student_access (test_id, student_id)
VALUES (@test_id, @student_id)
ON CONFLICT (test_id, student_id) DO NOTHING
RETURNING id;

