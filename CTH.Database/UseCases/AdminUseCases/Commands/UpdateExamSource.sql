UPDATE exam_source
SET
    year = COALESCE(@year, year),
    variant_number = COALESCE(@variant_number, variant_number),
    issuer = COALESCE(@issuer, issuer),
    notes = COALESCE(@notes, notes),
    updated_at = NOW()
WHERE id = @exam_source_id
RETURNING id;

