-- Category 2: Add Family Member
-- Remove affectedPerson
DELETE FROM Service_Form_Schema WHERE CategoryId = 2 AND FieldName = 'affectedPerson';

-- Update NIC field to text and set ValidationRules
UPDATE Service_Form_Schema 
SET FieldType = 'text', ValidationRules = '{"dataSource": "CitizenMaster", "autoPopulate": {"fullName": "fullName", "dateOfBirth": "dateOfBirth", "gender": "gender", "previousAddress": "previousAddress", "previousGnDivision": "previousGnDivision"}}'
WHERE CategoryId = 2 AND FieldName = 'nic';

-- Add fullName to Category 2
IF NOT EXISTS (SELECT 1 FROM Service_Form_Schema WHERE CategoryId = 2 AND FieldName = 'fullName')
BEGIN
    INSERT INTO Service_Form_Schema (CategoryId, FieldName, FieldLabel, FieldType, IsRequired, ValidationRules, DisplayOrder, FormSection, FormVersion, IsActive)
    VALUES (2, 'fullName', 'Full Name', 'text', 1, '{"readOnly": true}', 2, 'Request Details', '1.0', 1);
END

-- Ensure dateOfBirth, gender, previousAddress, previousGnDivision are properly configured
UPDATE Service_Form_Schema SET ValidationRules = '{"readOnly": true}' WHERE CategoryId = 2 AND FieldName IN ('dateOfBirth', 'gender', 'previousAddress', 'previousGnDivision');

-- Category 3: Remove Deceased Member
-- Remove excludeApplicant from affectedPerson
UPDATE Service_Form_Schema 
SET ValidationRules = '{"dataSource": "FamilyMembers", "displayField": "fullName", "valueField": "memberId", "autoPopulate": {"nic": "nic", "dateOfBirth": "dateOfBirth", "gender": "gender", "relationshipToHead": "relationshipToHead"}}'
WHERE CategoryId = 3 AND FieldName = 'affectedPerson';

-- Add Death Certificate Number field
IF NOT EXISTS (SELECT 1 FROM Service_Form_Schema WHERE CategoryId = 3 AND FieldName = 'deathCertificateNumber')
BEGIN
    INSERT INTO Service_Form_Schema (CategoryId, FieldName, FieldLabel, FieldType, IsRequired, DisplayOrder, FormSection, FormVersion, IsActive)
    VALUES (3, 'deathCertificateNumber', 'Death Certificate Number', 'text', 1, 6, 'Request Details', '1.0', 1);
END

-- Category 5: Split Family Card by Marriage
-- Replace affectedPerson with spouseNic
UPDATE Service_Form_Schema
SET FieldName = 'spouseNic', FieldLabel = 'Spouse NIC Number', FieldType = 'text'
WHERE CategoryId = 5 AND FieldName = 'affectedPerson';

-- Ensure relationshipInMarriage is correct (it already is, but just in case)
UPDATE Service_Form_Schema
SET ValidationRules = '{"readOnly": true, "computedField": {"source": "logged_in_citizen.gender", "condition": {"Male": "Wife", "Female": "Husband"}}}'
WHERE CategoryId = 5 AND FieldName = 'relationshipInMarriage';

-- Category 6: Remove Family Member
-- Remove excludeApplicant from affectedPerson
UPDATE Service_Form_Schema 
SET ValidationRules = '{"dataSource": "FamilyMembers", "displayField": "fullName", "valueField": "memberId", "autoPopulate": {"nic": "nic", "dateOfBirth": "dateOfBirth", "gender": "gender", "relationshipToHead": "relationshipToHead"}}'
WHERE CategoryId = 6 AND FieldName = 'affectedPerson';
