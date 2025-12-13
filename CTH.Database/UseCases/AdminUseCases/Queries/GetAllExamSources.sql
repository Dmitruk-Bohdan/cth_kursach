SELECT 
    id,
    year,
    variant_number,
    issuer,
    notes,
    created_at,
    updated_at
FROM exam_source
ORDER BY year DESC, variant_number;

