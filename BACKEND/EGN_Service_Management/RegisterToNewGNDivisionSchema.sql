-- Script to insert schema for "Register to New GN Division" (ServiceId = 2)

BEGIN TRANSACTION;
GO

DECLARE @ServiceId INT = 2;
DECLARE @CategoryId INT;

-- Create a dummy category since this service doesn't have categories but Service_Form_Schema requires CategoryId
INSERT INTO Service_Categories (ServiceId, CategoryName, GuidelineText)
VALUES (@ServiceId, 'Default', 'Guidelines for Register to New GN Division: Please fill out all required fields to request a transfer to a new GN division. Supporting documents are required.');

SET @CategoryId = SCOPE_IDENTITY();

-- Section 1: Current GN Information (Auto-Filled)
INSERT INTO Service_Form_Schema (CategoryId, FieldName, FieldLabel, FieldType, IsRequired, ValidationRules, DisplayOrder, FormSection, FormVersion, IsActive)
VALUES 
(@CategoryId, 'currentProvince', 'Current Province', 'text', 1, '{"readOnly": true, "dataSourceMapping": "Province"}', 1, 'Current GN Information (Auto-Filled)', '1.0', 1),
(@CategoryId, 'currentDistrict', 'Current District', 'text', 1, '{"readOnly": true, "dataSourceMapping": "District"}', 2, 'Current GN Information (Auto-Filled)', '1.0', 1),
(@CategoryId, 'currentDsDivision', 'Current DS Division', 'text', 1, '{"readOnly": true, "dataSourceMapping": "DsDivision"}', 3, 'Current GN Information (Auto-Filled)', '1.0', 1),
(@CategoryId, 'currentGnDivision', 'Current GN Division', 'text', 1, '{"readOnly": true, "dataSourceMapping": "GnDivision"}', 4, 'Current GN Information (Auto-Filled)', '1.0', 1),
(@CategoryId, 'currentResidentialAddress', 'Current Residential Address', 'textarea', 1, '{"readOnly": true, "dataSourceMapping": "Address"}', 5, 'Current GN Information (Auto-Filled)', '1.0', 1);

-- Section 2: New GN Information
INSERT INTO Service_Form_Schema (CategoryId, FieldName, FieldLabel, FieldType, IsRequired, ValidationRules, DisplayOrder, FormSection, FormVersion, IsActive)
VALUES 
(@CategoryId, 'newProvince', 'New Province', 'dropdown', 1, '{"dataSource": "province", "displayField": "name", "valueField": "id"}', 6, 'New GN Information', '1.0', 1),
(@CategoryId, 'newDistrict', 'New District', 'dropdown', 1, '{"dataSource": "districts", "dependsOn": "newProvince", "filterField": "provinceId", "displayField": "name", "valueField": "id"}', 7, 'New GN Information', '1.0', 1),
(@CategoryId, 'newDsDivision', 'New DS Division', 'dropdown', 1, '{"dataSource": "ds_divisions", "dependsOn": "newDistrict", "filterField": "districtId", "displayField": "name", "valueField": "id"}', 8, 'New GN Information', '1.0', 1),
(@CategoryId, 'newGnDivision', 'New GN Division', 'dropdown', 1, '{"dataSource": "gn_divisions", "dependsOn": "newDsDivision", "filterField": "dsDivisionId", "displayField": "name", "valueField": "id"}', 9, 'New GN Information', '1.0', 1),
(@CategoryId, 'newResidentialAddress', 'New Residential Address', 'textarea', 1, '{"minLength": 10, "maxLength": 500}', 10, 'New GN Information', '1.0', 1),
(@CategoryId, 'dateMovedToNewAddress', 'Date Moved to New Address', 'date', 1, '{"maxDate": "today"}', 11, 'New GN Information', '1.0', 1),
(@CategoryId, 'reasonForTransfer', 'Reason for GN Division Transfer', 'textarea', 1, '{"minLength": 20, "maxLength": 1000}', 12, 'New GN Information', '1.0', 1);

-- Section 3: Family Transfer Information
INSERT INTO Service_Form_Schema (CategoryId, FieldName, FieldLabel, FieldType, DropdownOptions, IsRequired, ValidationRules, DisplayOrder, FormSection, FormVersion, IsActive)
VALUES 
(@CategoryId, 'transferFamilyMembers', 'Transfer Family Members Together?', 'radio', '[{"label": "Yes", "value": "Yes"}, {"label": "No", "value": "No"}]', 1, NULL, 13, 'Family Transfer Information', '1.0', 1),
(@CategoryId, 'familyMembers', 'Family Members', 'checkbox', NULL, 0, '{"dataSource": "FamilyMembers", "displayField": "fullName", "valueField": "memberId", "excludeDeceased": true, "dependsOn": "transferFamilyMembers", "dependsOnValue": "Yes", "multiple": true}', 14, 'Family Transfer Information', '1.0', 1);

-- Section 4: Supporting Documents
INSERT INTO Service_Form_Schema (CategoryId, FieldName, FieldLabel, FieldType, IsRequired, ValidationRules, DisplayOrder, FormSection, FormVersion, IsActive)
VALUES 
(@CategoryId, 'supportingDocuments', 'Supporting Documents', 'file', 1, '{"maxFiles": 5, "allowedTypes": ["pdf", "jpg", "png"]}', 15, 'Supporting Documents', '1.0', 1);

COMMIT;
GO
