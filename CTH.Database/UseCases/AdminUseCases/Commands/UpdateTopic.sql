UPDATE topic
SET
    subject_id = COALESCE(@subject_id, subject_id),
    topic_name = COALESCE(@topic_name, topic_name),
    topic_code = COALESCE(@topic_code, topic_code),
    topic_parent_id = COALESCE(@topic_parent_id, topic_parent_id),
    is_active = COALESCE(@is_active, is_active),
    updated_at = NOW()
WHERE id = @topic_id
RETURNING id;

