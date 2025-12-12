SELECT
    ti.id,
    ti.subject_id,
    ti.topic_id,
    ti.task_type,
    ti.difficulty,
    ti.statement,
    ti.explanation,
    ti.is_active,
    t.topic_name,
    t.topic_code
FROM task_item ti
LEFT JOIN topic t ON t.id = ti.topic_id
WHERE ti.subject_id = @subject_id
  AND ti.is_active = TRUE
  AND (
    @search_query IS NULL
    OR @search_query = ''
    OR (
      -- Поиск по ID (если search_query - число)
      (@search_is_number = TRUE AND ti.id = CAST(@search_query AS BIGINT))
      OR
      -- Поиск по словам в statement (ILIKE для регистронезависимого поиска)
      (ti.statement ILIKE '%' || @search_query || '%')
    )
  )
ORDER BY 
  -- Сначала точное совпадение по ID, затем по тексту
  CASE 
    WHEN @search_is_number = TRUE AND @search_query IS NOT NULL AND @search_query != '' AND ti.id = CAST(@search_query AS BIGINT) THEN 1
    WHEN @search_query IS NOT NULL AND @search_query != '' AND ti.statement ILIKE '%' || @search_query || '%' THEN 2
    ELSE 3
  END,
  t.topic_name NULLS LAST, 
  ti.id;

