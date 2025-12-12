SELECT
    tsa.id,
    tsa.test_id,
    tsa.student_id,
    tsa.created_at,
    u.id as user_id,
    u.user_name,
    u.email
FROM test_student_access tsa
JOIN user_account u ON u.id = tsa.student_id
WHERE tsa.test_id = @test_id
ORDER BY tsa.created_at DESC;

