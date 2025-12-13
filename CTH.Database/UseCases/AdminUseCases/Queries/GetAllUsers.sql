SELECT 
    ua.id,
    ua.user_name,
    ua.email,
    r.role_name,
    r.id AS role_id,
    ua.last_login_at,
    ua.created_at
FROM user_account ua
JOIN role r ON r.id = ua.role_type_id
ORDER BY ua.created_at DESC;

