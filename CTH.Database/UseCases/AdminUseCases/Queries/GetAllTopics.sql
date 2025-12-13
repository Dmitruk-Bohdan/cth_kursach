SELECT 
    t.id,
    t.subject_id,
    s.subject_name,
    t.topic_name,
    t.topic_code,
    t.topic_parent_id,
    pt.topic_name AS parent_topic_name,
    t.is_active,
    t.created_at,
    t.updated_at
FROM topic t
JOIN subject s ON s.id = t.subject_id
LEFT JOIN topic pt ON pt.id = t.topic_parent_id
WHERE (@subject_id IS NULL OR t.subject_id = @subject_id)
ORDER BY s.subject_name, t.topic_name;

