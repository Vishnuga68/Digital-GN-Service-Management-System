-- 1. Remove CategoryName Income / Social Status Confirmation from Service_Categories
DELETE FROM [dbo].[Service_Categories] WHERE [CategoryName] = 'Income / Social Status Confirmation';

-- 2. Update Document Request Form Schema
DECLARE @DocSvcId INT;
SELECT @DocSvcId = [ServiceId] FROM [dbo].[Services] WHERE [ServiceName] = 'Document Request';

-- Use 'Residence Certificate' as the main category for the generic form (renamed to avoid confusion if needed, but we won't rename to avoid breaking anything)
-- Actually, the prompt doesn't explicitly ask to rename categories, just "move request type into this section rename it as certificate type".
-- So we will create a NEW category called "Document Request Form" and delete the others? 
-- "do not affect any oher data and any other table strictly". Let's use the generic "Residence Certificate" CategoryId and update its schema as the master form.
DECLARE @CatId INT;
SELECT @CatId = [CategoryId] FROM [dbo].[Service_Categories] WHERE [ServiceId] = @DocSvcId AND [CategoryName] = 'Residence Certificate';

-- Delete existing schema for this category
DELETE FROM [dbo].[Service_Form_Schema] WHERE [CategoryId] = @CatId;

-- Insert the new schema
INSERT INTO [dbo].[Service_Form_Schema] (CategoryId, FieldName, FieldLabel, FieldType, DropdownOptions, IsRequired, ValidationRules, DisplayOrder, FormSection) VALUES
-- SECTION 1: PERSONAL INFORMATION
(@CatId, 'applicantType', 'Whom is this request for?', 'applicant-type', NULL, 1, NULL, 1, 'Personal Information'),
(@CatId, 'childSelection', 'Select Child', 'dropdown', NULL, 1, '{"conditionalVisibility": {"field": "applicantType", "value": "Child under 18"}, "dataSource": "FamilyMembers", "filterCriteria": {"maxAge": 18}, "displayField": "fullName-nic", "autoPopulate": {"childGender": "gender", "childDob": "dob", "childAge": "age"}}', 2, 'Personal Information'),
(@CatId, 'childGender', 'Gender', 'text', NULL, 0, '{"conditionalVisibility": {"field": "applicantType", "value": "Child under 18"}, "readOnly": true}', 3, 'Personal Information'),
(@CatId, 'childDob', 'Date of Birth', 'date', NULL, 0, '{"conditionalVisibility": {"field": "applicantType", "value": "Child under 18"}, "readOnly": true}', 4, 'Personal Information'),
(@CatId, 'childAge', 'Age', 'text', NULL, 0, '{"conditionalVisibility": {"field": "applicantType", "value": "Child under 18"}, "readOnly": true}', 5, 'Personal Information'),

-- SECTION 2: REQUEST DETAILS
(@CatId, 'certificateType', 'Certificate Type', 'dropdown', '[{"label":"Residence Certificate","value":"residence"},{"label":"Character Certificate","value":"character"},{"label":"Temporary Residence Certificate","value":"temp_residence"},{"label":"Death Proof","value":"death"},{"label":"Other Certificate Request","value":"other"}]', 1, NULL, 6, 'Request Details'),
(@CatId, 'deliveryOptionAll', 'Delivery Option', 'dropdown', '[{"label":"Collect from GN Office","value":"collect"},{"label":"Digital Copy","value":"digital"}]', 1, '{"conditionalVisibility": {"field": "certificateType", "in": ["residence", "character"]}}', 7, 'Request Details'),
(@CatId, 'deliveryOptionPhysical', 'Delivery Option', 'dropdown', '[{"label":"Collect from GN Office","value":"collect"}]', 1, '{"conditionalVisibility": {"field": "certificateType", "in": ["temp_residence", "death", "other"]}}', 8, 'Request Details'),
(@CatId, 'purpose', 'Purpose of Request', 'textarea', NULL, 1, NULL, 9, 'Request Details'),
(@CatId, 'requiredFor', 'Certificate Required For', 'dropdown', '[{"label":"Personal Use","value":"Personal Use"},{"label":"School / University","value":"School / University"},{"label":"Employment","value":"Employment"},{"label":"Bank Loan","value":"Bank Loan"},{"label":"Passport / Visa","value":"Passport / Visa"},{"label":"Court Purpose","value":"Court Purpose"},{"label":"Other","value":"Other"}]', 1, NULL, 10, 'Request Details'),
(@CatId, 'language', 'Language Preference', 'dropdown', '[{"label":"Sinhala","value":"Sinhala"},{"label":"Tamil","value":"Tamil"},{"label":"English","value":"English"}]', 1, NULL, 11, 'Request Details'),
(@CatId, 'urgency', 'Urgency Level', 'dropdown', '[{"label":"Normal","value":"Normal"},{"label":"Urgent","value":"Urgent"}]', 1, NULL, 12, 'Request Details'),
(@CatId, 'remarks', 'Additional Remarks', 'textarea', NULL, 0, NULL, 13, 'Request Details'),

-- SECTION 3: SUPPORTING DOCUMENTS
(@CatId, 'attachments', 'File Upload', 'file', NULL, 1, '{"multiple": true, "accept": ".pdf,.jpg,.png"}', 14, 'Supporting Documents');

-- Update the generic category name to "Document Request Form" to be user friendly
UPDATE [dbo].[Service_Categories] SET [CategoryName] = 'Document Request Form', [GuidelineText] = 'Upload required documents for verification.' WHERE [CategoryId] = @CatId;

-- Delete the other duplicate categories under Document Request Service to avoid confusion since we moved it into the form
DELETE FROM [dbo].[Service_Categories] WHERE [ServiceId] = @DocSvcId AND [CategoryId] != @CatId;
