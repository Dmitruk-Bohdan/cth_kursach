SELECT 
    test.id,
    test.subject_id,
    s.subject_name,
    test.test_kind,
    test.title,
    test.author_id,
    ua.user_name AS author_name,
    test.is_published,
    test.is_public,
    test.is_state_archive,
    test.created_at,
    test.updated_at
FROM test
JOIN subject s ON s.id = test.subject_id
LEFT JOIN user_account ua ON ua.id = test.author_id
WHERE 
    (@subject_id IS NULL OR test.subject_id = @subject_id)
    AND (@test_kind IS NULL OR test.test_kind = @test_kind)
    AND (@author_id IS NULL OR test.author_id = @author_id)
    AND (@is_published IS NULL OR test.is_published = @is_published)
    AND (@is_state_archive IS NULL OR test.is_state_archive = @is_state_archive)
    AND (@search IS NULL OR test.title ILIKE '%' || @search || '%')
ORDER BY test.created_at DESC;

