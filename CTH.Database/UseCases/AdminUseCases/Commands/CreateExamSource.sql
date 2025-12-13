INSERT INTO exam_source (year, variant_number, issuer, notes, created_at, updated_at)
VALUES (@year, @variant_number, @issuer, @notes, NOW(), NOW())
RETURNING id;

