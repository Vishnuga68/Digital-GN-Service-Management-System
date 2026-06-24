-- UpdateFamilyCardFormSchema.sql

DECLARE @AddNewbornId INT, @AddFamilyMemberId INT, @RemoveDeceasedId INT, @ChangeHeadId INT, @SplitCardId INT, @RemoveMigratedId INT;

SELECT @AddNewbornId = [CategoryId] FROM [dbo].[Service_Categories] WHERE [CategoryName] = 'Add Newborn to Family Card';
SELECT @AddFamilyMemberId = [CategoryId] FROM [dbo].[Service_Categories] WHERE [CategoryName] = 'Add Family Member';
SELECT @RemoveDeceasedId = [CategoryId] FROM [dbo].[Service_Categories] WHERE [CategoryName] = 'Remove Deceased Family Member';
SELECT @ChangeHeadId = [CategoryId] FROM [dbo].[Service_Categories] WHERE [CategoryName] = 'Change Family Head';
SELECT @SplitCardId = [CategoryId] FROM [dbo].[Service_Categories] WHERE [CategoryName] = 'Split Family Card by Marriage';
SELECT @RemoveMigratedId = [CategoryId] FROM [dbo].[Service_Categories] WHERE [CategoryName] IN ('Remove Migrated Family Member', 'Remove Family Member');

-- Update Category Name
UPDATE [dbo].[Service_Categories] SET [CategoryName] = 'Remove Family Member' WHERE [CategoryId] = @RemoveMigratedId;

-- Update Guidelines
UPDATE [dbo].[Service_Categories] SET [GuidelineText] = 'Upload Child''s Birth Certificate.' WHERE [CategoryId] = @AddNewbornId;
UPDATE [dbo].[Service_Categories] SET [GuidelineText] = 'Upload NIC Copy and Birth Certificate. Marriage Certificate is optional.' WHERE [CategoryId] = @AddFamilyMemberId;
UPDATE [dbo].[Service_Categories] SET [GuidelineText] = 'Upload Death Certificate.' WHERE [CategoryId] = @RemoveDeceasedId;
UPDATE [dbo].[Service_Categories] SET [GuidelineText] = 'Upload Supporting Document.' WHERE [CategoryId] = @ChangeHeadId;
UPDATE [dbo].[Service_Categories] SET [GuidelineText] = 'Upload Marriage Certificate and Address Proof.' WHERE [CategoryId] = @SplitCardId;
UPDATE [dbo].[Service_Categories] SET [GuidelineText] = 'Upload Proof of Residence. Transfer Documents are optional.' WHERE [CategoryId] = @RemoveMigratedId;

-- Clear old schema for these categories
DELETE FROM [dbo].[Service_Form_Schema] WHERE [CategoryId] IN (@AddNewbornId, @AddFamilyMemberId, @RemoveDeceasedId, @ChangeHeadId, @SplitCardId, @RemoveMigratedId);

-- INSERT CONFIGURATIONS

-- 1. ADD NEWBORN TO FAMILY CARD
INSERT INTO [dbo].[Service_Form_Schema] (CategoryId, FieldName, FieldLabel, FieldType, DropdownOptions, IsRequired, ValidationRules, DisplayOrder) VALUES

(@AddNewbornId, 'childFullName', 'Child Full Name', 'text', NULL, 1, NULL, 6),
(@AddNewbornId, 'gender', 'Gender', 'dropdown', '[{"label":"Male","value":"Male"},{"label":"Female","value":"Female"}]', 1, NULL, 7),
(@AddNewbornId, 'dateOfBirth', 'Date Of Birth', 'date', NULL, 1, NULL, 8),
(@AddNewbornId, 'placeOfBirth', 'Place Of Birth', 'text', NULL, 1, NULL, 9),
(@AddNewbornId, 'birthCertificateNumber', 'Birth Certificate Number', 'text', NULL, 1, NULL, 10),
(@AddNewbornId, 'relationshipOfChild', 'Relationship of Child to You', 'dropdown', '[{"label":"Son","value":"Son"},{"label":"Daughter","value":"Daughter"}]', 1, NULL, 11);

-- 2. ADD FAMILY MEMBER
INSERT INTO [dbo].[Service_Form_Schema] (CategoryId, FieldName, FieldLabel, FieldType, DropdownOptions, IsRequired, ValidationRules, DisplayOrder) VALUES

(@AddFamilyMemberId, 'nic', 'NIC', 'nic_lookup', NULL, 1, '{"dataSource": "CitizenMaster", "autoPopulate": {"fullName": "fullName", "dateOfBirth": "dateOfBirth", "gender": "gender", "previousAddress": "previousAddress", "previousGnDivision": "previousGnDivision"}}', 6),
(@AddFamilyMemberId, 'fullName', 'Full Name', 'text', NULL, 1, '{"readOnly": true}', 7),
(@AddFamilyMemberId, 'dateOfBirth', 'Date Of Birth', 'date', NULL, 1, '{"readOnly": true}', 8),
(@AddFamilyMemberId, 'gender', 'Gender', 'text', NULL, 1, '{"readOnly": true}', 9),
(@AddFamilyMemberId, 'previousAddress', 'Previous Address', 'textarea', NULL, 1, '{"readOnly": true}', 10),
(@AddFamilyMemberId, 'previousGnDivision', 'Previous GN Division', 'text', NULL, 1, '{"readOnly": true}', 11),
(@AddFamilyMemberId, 'reasonForAdding', 'Reason for Adding Member', 'dropdown', '[{"label":"Returning Family Member","value":"Returning Family Member"},{"label":"Dependent Parent","value":"Dependent Parent"},{"label":"Child","value":"Child"},{"label":"Legal Guardian Change","value":"Legal Guardian Change"},{"label":"Other","value":"Other"}]', 1, NULL, 12),
(@AddFamilyMemberId, 'otherReason', 'Other Reason', 'textarea', NULL, 0, '{"conditionalVisibility": {"field": "reasonForAdding", "operator": "equals", "value": "Other"}}', 13);

-- 3. REMOVE DECEASED FAMILY MEMBER
INSERT INTO [dbo].[Service_Form_Schema] (CategoryId, FieldName, FieldLabel, FieldType, DropdownOptions, IsRequired, ValidationRules, DisplayOrder) VALUES

(@RemoveDeceasedId, 'affectedPerson', 'Affected Person', 'dropdown', NULL, 1, '{"dataSource": "FamilyMembers", "filterCriteria": {"excludeApplicant": true}, "displayField": "fullName", "valueField": "memberId", "autoPopulate": {"nic": "nic", "dateOfBirth": "dateOfBirth", "gender": "gender", "relationshipToHead": "relationshipToHead"}}', 6),
(@RemoveDeceasedId, 'nic', 'NIC', 'text', NULL, 1, '{"readOnly": true}', 7),
(@RemoveDeceasedId, 'dateOfBirth', 'Date Of Birth', 'date', NULL, 1, '{"readOnly": true}', 8),
(@RemoveDeceasedId, 'gender', 'Gender', 'text', NULL, 1, '{"readOnly": true}', 9),
(@RemoveDeceasedId, 'relationshipToHead', 'Relationship To Family Head', 'text', NULL, 1, '{"readOnly": true}', 10),
(@RemoveDeceasedId, 'dateOfDeath', 'Date Of Death', 'date', NULL, 1, NULL, 11),
(@RemoveDeceasedId, 'placeOfDeath', 'Place Of Death', 'text', NULL, 1, NULL, 12);

-- 4. CHANGE FAMILY HEAD
INSERT INTO [dbo].[Service_Form_Schema] (CategoryId, FieldName, FieldLabel, FieldType, DropdownOptions, IsRequired, ValidationRules, DisplayOrder) VALUES

(@ChangeHeadId, 'currentFamilyHead', 'Current Family Head', 'text', NULL, 1, '{"readOnly": true, "autoPopulate": {"source": "CurrentFamilyCard", "field": "familyHeadName"}}', 6),
(@ChangeHeadId, 'newFamilyHead', 'New Family Head', 'dropdown', NULL, 1, '{"dataSource": "FamilyMembers", "filterCriteria": {"minAge": 18}, "displayField": "fullName", "valueField": "memberId", "autoPopulate": {"nic": "nic", "dateOfBirth": "dateOfBirth", "gender": "gender", "relationshipToHead": "relationshipToHead"}}', 7),
(@ChangeHeadId, 'nic', 'NIC', 'text', NULL, 1, '{"readOnly": true}', 8),
(@ChangeHeadId, 'dateOfBirth', 'Date Of Birth', 'date', NULL, 1, '{"readOnly": true}', 9),
(@ChangeHeadId, 'gender', 'Gender', 'text', NULL, 1, '{"readOnly": true}', 10),
(@ChangeHeadId, 'relationshipToHead', 'Relationship To Current Head', 'text', NULL, 1, '{"readOnly": true}', 11),
(@ChangeHeadId, 'reason', 'Reason', 'dropdown', '[{"label":"Death Of Current Head","value":"Death Of Current Head"},{"label":"Migration","value":"Migration"},{"label":"Marriage","value":"Marriage"},{"label":"Separation","value":"Separation"},{"label":"Incapacity","value":"Incapacity"},{"label":"Other","value":"Other"}]', 1, NULL, 12);

-- 5. SPLIT FAMILY CARD BY MARRIAGE
INSERT INTO [dbo].[Service_Form_Schema] (CategoryId, FieldName, FieldLabel, FieldType, DropdownOptions, IsRequired, ValidationRules, DisplayOrder) VALUES

(@SplitCardId, 'affectedPerson', 'Affected Person', 'nic_lookup', NULL, 1, '{"dataSource": "CitizenMaster", "autoPopulate": {"fullName": "fullName", "gender": "gender", "gnDivision": "gnDivision"}}', 6),
(@SplitCardId, 'fullName', 'Full Name', 'text', NULL, 1, '{"readOnly": true}', 7),
(@SplitCardId, 'gender', 'Gender', 'text', NULL, 1, '{"readOnly": true}', 8),
(@SplitCardId, 'gnDivision', 'GN Division', 'text', NULL, 1, '{"readOnly": true}', 9),
(@SplitCardId, 'relationshipInMarriage', 'Relationship In Marriage', 'text', NULL, 1, '{"readOnly": true, "computedField": {"source": "logged_in_citizen.gender", "condition": {"Male": "Wife", "Female": "Husband"}}}', 10),
(@SplitCardId, 'marriageCertificateNumber', 'Marriage Certificate Number', 'text', NULL, 1, NULL, 11),
(@SplitCardId, 'marriageDate', 'Marriage Date', 'date', NULL, 1, NULL, 12),
(@SplitCardId, 'newAddress', 'New Address', 'textarea', NULL, 1, NULL, 13);

-- 6. REMOVE FAMILY MEMBER
INSERT INTO [dbo].[Service_Form_Schema] (CategoryId, FieldName, FieldLabel, FieldType, DropdownOptions, IsRequired, ValidationRules, DisplayOrder) VALUES

(@RemoveMigratedId, 'affectedPerson', 'Affected Person', 'dropdown', NULL, 1, '{"dataSource": "FamilyMembers", "filterCriteria": {"excludeApplicant": true}, "displayField": "fullName", "valueField": "memberId", "autoPopulate": {"nic": "nic", "dateOfBirth": "dateOfBirth", "gender": "gender", "relationshipToHead": "relationshipToHead"}}', 6),
(@RemoveMigratedId, 'nic', 'NIC', 'text', NULL, 1, '{"readOnly": true}', 7),
(@RemoveMigratedId, 'dateOfBirth', 'Date Of Birth', 'date', NULL, 1, '{"readOnly": true}', 8),
(@RemoveMigratedId, 'gender', 'Gender', 'text', NULL, 1, '{"readOnly": true}', 9),
(@RemoveMigratedId, 'relationshipToHead', 'Relationship To Family Head', 'text', NULL, 1, '{"readOnly": true}', 10),
(@RemoveMigratedId, 'reasonForRemoval', 'Reason for Removal', 'textarea', NULL, 1, NULL, 11),
(@RemoveMigratedId, 'newGnDivision', 'New GN Division', 'text', NULL, 1, NULL, 12),
(@RemoveMigratedId, 'newAddress', 'New Address', 'textarea', NULL, 1, NULL, 13);

GO
