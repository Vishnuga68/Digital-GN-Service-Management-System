-- Shift dateOfDeath and placeOfDeath to make room for deathCertificateNumber after relationshipToHead (DisplayOrder 10)
UPDATE Service_Form_Schema SET DisplayOrder = 13 WHERE CategoryId = 3 AND FieldName = 'placeOfDeath';
UPDATE Service_Form_Schema SET DisplayOrder = 12 WHERE CategoryId = 3 AND FieldName = 'dateOfDeath';

-- Set deathCertificateNumber to immediately follow relationshipToHead
UPDATE Service_Form_Schema SET DisplayOrder = 11 WHERE CategoryId = 3 AND FieldName = 'deathCertificateNumber';

-- Change spouseNic fieldType back to nic_lookup for lookup functionality
UPDATE Service_Form_Schema SET FieldType = 'nic_lookup' WHERE CategoryId = 5 AND FieldName = 'spouseNic';
