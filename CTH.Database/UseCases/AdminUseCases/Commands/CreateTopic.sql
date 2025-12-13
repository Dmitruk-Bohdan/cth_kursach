INSERT INTO topic (subject_id, topic_name, topic_code, topic_parent_id, is_active, created_at, updated_at)
VALUES (@subject_id, @topic_name, @topic_code, @topic_parent_id, @is_active, NOW(), NOW())
RETURNING id;

