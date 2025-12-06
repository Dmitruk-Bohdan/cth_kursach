SELECT
    tt.id            AS test_task_id,
    tt.test_id,
    tt.task_id,
    tt.position,
    tt.weight,
    ti.task_type,
    ti.statement,
    ti.difficulty,
    ti.explanation,
    ti.correct_answer
FROM test_task tt
JOIN task_item ti ON ti.id = tt.task_id
WHERE tt.test_id = @test_id
ORDER BY tt.position;
