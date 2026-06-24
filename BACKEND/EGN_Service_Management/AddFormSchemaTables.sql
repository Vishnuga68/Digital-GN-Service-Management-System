-- 1. Create Parent Headings Table
CREATE TABLE [dbo].[Service_Parent_Headings] (
    [ParentHeadingId] INT IDENTITY(1,1) PRIMARY KEY,
    [ParentHeadingName] NVARCHAR(255) NOT NULL,
    [Icon] NVARCHAR(255) NULL,
    [Description] NVARCHAR(MAX) NULL
);
GO

-- Insert Parent Headings
INSERT INTO [dbo].[Service_Parent_Headings] ([ParentHeadingName], [Icon], [Description]) VALUES 
('Registry Updates', '/assets/icons/registry.png', 'Update your registry and family information'),
('Document Services', '/assets/icons/request.png', 'Request or verify official documents'),
('Welfare & Assistance', '/assets/icons/welfare.png', 'Apply for government welfare programs'),
('Complaints & Grievances', '/assets/icons/complaints.png', 'Submit formal complaints'),
('General Requests', '/assets/icons/other.png', 'Other miscellaneous requests');
GO

-- 2. Alter Services Table
ALTER TABLE [dbo].[Services] ADD [ParentHeadingId] INT NULL;
ALTER TABLE [dbo].[Services] ADD [Icon] NVARCHAR(255) NULL;
ALTER TABLE [dbo].[Services] ADD [Description] NVARCHAR(MAX) NULL;
GO

ALTER TABLE [dbo].[Services] 
    ADD CONSTRAINT [FK_Services_Parent_Headings] FOREIGN KEY ([ParentHeadingId]) REFERENCES [dbo].[Service_Parent_Headings]([ParentHeadingId]);
GO

-- Update existing services with parent heading, icon, and description
DECLARE @ParentId INT;

SELECT @ParentId = [ParentHeadingId] FROM [dbo].[Service_Parent_Headings] WHERE [ParentHeadingName] = 'Registry Updates';
UPDATE [dbo].[Services] SET [ParentHeadingId] = @ParentId, [Icon] = '/assets/icons/family.png', [Description] = 'Manage household and family card records including newborn registration, family updates, migration updates, marriage-related updates, and family head management.' WHERE [ServiceName] = 'Family Card Services';
UPDATE [dbo].[Services] SET [ParentHeadingId] = @ParentId, [Icon] = '/assets/icons/gn-transfer.png', [Description] = 'Update your official residency information when relocating to a different GN division.' WHERE [ServiceName] = 'Register to New GN Division';
UPDATE [dbo].[Services] SET [ParentHeadingId] = @ParentId, [Icon] = '/assets/icons/vote.png', [Description] = 'Register as a voter after reaching 18 years of age and getting eligibility for elections.' WHERE [ServiceName] = 'Voter Registration (18+)';

SELECT @ParentId = [ParentHeadingId] FROM [dbo].[Service_Parent_Headings] WHERE [ParentHeadingName] = 'Document Services';
UPDATE [dbo].[Services] SET [ParentHeadingId] = @ParentId, [Icon] = '/assets/icons/document-request.png', [Description] = 'Request certified copies of your official documents, certificates, or letters of residency.' WHERE [ServiceName] = 'Document Request';
UPDATE [dbo].[Services] SET [ParentHeadingId] = @ParentId, [Icon] = '/assets/icons/document-verify.png', [Description] = 'Get your official documents verified and stamped for legal or third-party requirements.' WHERE [ServiceName] = 'Document Verification';

SELECT @ParentId = [ParentHeadingId] FROM [dbo].[Service_Parent_Headings] WHERE [ParentHeadingName] = 'Welfare & Assistance';
UPDATE [dbo].[Services] SET [ParentHeadingId] = @ParentId, [Icon] = '/assets/icons/family.png', [Description] = 'Apply for welfare and assistance programs verified through Grama Niladhari office.' WHERE [ServiceName] = 'Welfare & Assistance';

SELECT @ParentId = [ParentHeadingId] FROM [dbo].[Service_Parent_Headings] WHERE [ParentHeadingName] = 'Complaints & Grievances';
UPDATE [dbo].[Services] SET [ParentHeadingId] = @ParentId, [Icon] = '/assets/icons/complaint-new.png', [Description] = 'Lodge a formal grievance regarding local services, administrative issues, or community matters.' WHERE [ServiceName] = 'Complaints & Grievances';

SELECT @ParentId = [ParentHeadingId] FROM [dbo].[Service_Parent_Headings] WHERE [ParentHeadingName] = 'General Requests';
UPDATE [dbo].[Services] SET [ParentHeadingId] = @ParentId, [Icon] = '/assets/icons/other-new.png', [Description] = 'Submit any specialized requests or general inquiries that do not fall into standard categories.' WHERE [ServiceName] = 'Other Requests';

-- 3. Alter Service_Categories to add Guidelines
ALTER TABLE [dbo].[Service_Categories] ADD [GuidelineText] NVARCHAR(MAX) NULL;
GO

UPDATE [dbo].[Service_Categories] SET [GuidelineText] = 'Upload Child''s Birth Certificate and parents'' NICs.' WHERE [CategoryName] = 'Add Newborn to Family Card';
UPDATE [dbo].[Service_Categories] SET [GuidelineText] = 'Upload relevant documents proving relationship/adoption.' WHERE [CategoryName] = 'Add Family Member';
UPDATE [dbo].[Service_Categories] SET [GuidelineText] = 'A valid Death Certificate is mandatory.' WHERE [CategoryName] = 'Remove Deceased Family Member';
UPDATE [dbo].[Service_Categories] SET [GuidelineText] = 'Upload consent letters or death/migration proof.' WHERE [CategoryName] = 'Change Family Head';
UPDATE [dbo].[Service_Categories] SET [GuidelineText] = 'A valid Marriage Certificate is mandatory.' WHERE [CategoryName] = 'Split Family Card by Marriage';
UPDATE [dbo].[Service_Categories] SET [GuidelineText] = 'Upload proof of new residence or migration documents.' WHERE [CategoryName] = 'Remove Migrated Family Member';
GO

-- 4. Create Service_Form_Schema Table
CREATE TABLE [dbo].[Service_Form_Schema] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [CategoryId] INT NOT NULL,
    [FieldName] NVARCHAR(255) NOT NULL,
    [FieldLabel] NVARCHAR(255) NOT NULL,
    [FieldType] NVARCHAR(50) NOT NULL, -- text, number, date, dropdown, textarea, applicant-type
    [DropdownOptions] NVARCHAR(MAX) NULL, -- JSON string for dropdown options
    [IsRequired] BIT NOT NULL DEFAULT 0,
    [ValidationRules] NVARCHAR(MAX) NULL, -- JSON rules like min/max length
    [DisplayOrder] INT NOT NULL,
    [FormSection] NVARCHAR(100) NULL, -- e.g., 'Request Details', 'Applicant Details'
    [FormVersion] NVARCHAR(50) NOT NULL DEFAULT '1.0',
    [IsActive] BIT NOT NULL DEFAULT 1,
    CONSTRAINT [FK_Service_Form_Schema_Categories] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Service_Categories]([CategoryId]) ON DELETE CASCADE
);

-- 5. SEED DATA for Form Schema
DECLARE @CatId INT;

-- ADD NEWBORN
SELECT @CatId = [CategoryId] FROM [dbo].[Service_Categories] WHERE [CategoryName] = 'Add Newborn to Family Card';
INSERT INTO [dbo].[Service_Form_Schema] (CategoryId, FieldName, FieldLabel, FieldType, DropdownOptions, IsRequired, DisplayOrder) VALUES
(@CatId, 'newbornRelationship', 'Relationship to Family Head', 'text', NULL, 0, 1),
(@CatId, 'newbornName', 'Child Full Name', 'text', NULL, 0, 2),
(@CatId, 'newbornDob', 'Date of Birth', 'date', NULL, 0, 3),
(@CatId, 'newbornGender', 'Gender', 'dropdown', '[{"label":"Male","value":"Male"},{"label":"Female","value":"Female"}]', 0, 4),
(@CatId, 'motherNic', 'Mother NIC', 'text', NULL, 0, 5),
(@CatId, 'fatherNic', 'Father NIC', 'text', NULL, 0, 6),
(@CatId, 'newbornNotes', 'Additional Notes', 'textarea', NULL, 0, 7);

-- ADD FAMILY MEMBER
SELECT @CatId = [CategoryId] FROM [dbo].[Service_Categories] WHERE [CategoryName] = 'Add Family Member';
INSERT INTO [dbo].[Service_Form_Schema] (CategoryId, FieldName, FieldLabel, FieldType, DropdownOptions, IsRequired, DisplayOrder) VALUES
(@CatId, 'newMemberNic', 'New Member NIC', 'text', NULL, 0, 1),
(@CatId, 'newMemberName', 'Full Name', 'text', NULL, 0, 2),
(@CatId, 'newMemberRelationship', 'Relationship to Family Head', 'text', NULL, 0, 3),
(@CatId, 'newMemberReason', 'Reason for Addition', 'dropdown', '[{"label":"Adoption","value":"adoption"},{"label":"Other","value":"other"}]', 0, 4);

-- REMOVE DECEASED
SELECT @CatId = [CategoryId] FROM [dbo].[Service_Categories] WHERE [CategoryName] = 'Remove Deceased Family Member';
INSERT INTO [dbo].[Service_Form_Schema] (CategoryId, FieldName, FieldLabel, FieldType, DropdownOptions, IsRequired, DisplayOrder) VALUES
(@CatId, 'deceasedMemberNic', 'Deceased Member NIC', 'text', NULL, 0, 1),
(@CatId, 'dateOfDeath', 'Date of Death', 'date', NULL, 0, 2),
(@CatId, 'deceasedRemarks', 'Remarks', 'textarea', NULL, 0, 3);

-- CHANGE FAMILY HEAD
SELECT @CatId = [CategoryId] FROM [dbo].[Service_Categories] WHERE [CategoryName] = 'Change Family Head';
INSERT INTO [dbo].[Service_Form_Schema] (CategoryId, FieldName, FieldLabel, FieldType, DropdownOptions, IsRequired, DisplayOrder) VALUES
(@CatId, 'currentFamilyHead', 'Current Family Head', 'text', NULL, 0, 1),
(@CatId, 'newFamilyHead', 'New Family Head', 'text', NULL, 0, 2),
(@CatId, 'changeHeadReason', 'Reason', 'dropdown', '[{"label":"Death","value":"death"},{"label":"Migration","value":"migration"},{"label":"Divorce / Separation","value":"divorce"},{"label":"Voluntary Transfer","value":"transfer"},{"label":"Other","value":"other"}]', 0, 3);

-- SPLIT FAMILY CARD
SELECT @CatId = [CategoryId] FROM [dbo].[Service_Categories] WHERE [CategoryName] = 'Split Family Card by Marriage';
INSERT INTO [dbo].[Service_Form_Schema] (CategoryId, FieldName, FieldLabel, FieldType, DropdownOptions, IsRequired, DisplayOrder) VALUES
(@CatId, 'groomNic', 'Groom NIC', 'text', NULL, 0, 1),
(@CatId, 'brideNic', 'Bride NIC', 'text', NULL, 0, 2),
(@CatId, 'existingFamilyCardNumber', 'Existing Family Card Number', 'text', NULL, 0, 3),
(@CatId, 'contactNumber', 'Contact Number', 'text', NULL, 0, 4),
(@CatId, 'newAddress', 'New Address (if applicable)', 'textarea', NULL, 0, 5);

-- REMOVE MIGRATED
SELECT @CatId = [CategoryId] FROM [dbo].[Service_Categories] WHERE [CategoryName] = 'Remove Migrated Family Member';
INSERT INTO [dbo].[Service_Form_Schema] (CategoryId, FieldName, FieldLabel, FieldType, DropdownOptions, IsRequired, DisplayOrder) VALUES
(@CatId, 'migratedNic', 'Citizen NIC', 'text', NULL, 0, 1),
(@CatId, 'migrationDate', 'Migration Date', 'date', NULL, 0, 2),
(@CatId, 'migrationReason', 'Reason', 'dropdown', '[{"label":"Moved to Another GN Division","value":"another-gn"},{"label":"Abroad permanent residence","value":"abroad"}]', 0, 3),
(@CatId, 'migratedNewAddress', 'New Address', 'textarea', NULL, 0, 4);

-- COMPLAINTS
SELECT @CatId = [CategoryId] FROM [dbo].[Service_Categories] WHERE [CategoryName] = 'Incorrect Record Complaint (Family Card / Address)';
INSERT INTO [dbo].[Service_Form_Schema] (CategoryId, FieldName, FieldLabel, FieldType, DropdownOptions, IsRequired, DisplayOrder) VALUES
(@CatId, 'subject', 'Complaint Title', 'text', NULL, 1, 1),
(@CatId, 'complaintCategory', 'Complaint Category', 'dropdown', '[{"label":"Public Service","value":"public-service"},{"label":"Infrastructure","value":"infrastructure"},{"label":"Environment","value":"environment"},{"label":"Social Issue","value":"social-issue"},{"label":"Other","value":"other"}]', 1, 2),
(@CatId, 'priorityLevel', 'Priority Level', 'dropdown', '[{"label":"Low","value":"low"},{"label":"Medium","value":"medium"},{"label":"High","value":"high"}]', 0, 3),
(@CatId, 'incidentDate', 'Date of Incident', 'date', NULL, 0, 4),
(@CatId, 'incidentLocation', 'Location', 'text', NULL, 1, 5),
(@CatId, 'description', 'Complaint Description', 'textarea', NULL, 1, 6);

-- WELFARE
SELECT @CatId = [CategoryId] FROM [dbo].[Service_Categories] WHERE [CategoryName] = 'Aswesuma Welfare Benefit Scheme';
INSERT INTO [dbo].[Service_Form_Schema] (CategoryId, FieldName, FieldLabel, FieldType, DropdownOptions, IsRequired, DisplayOrder) VALUES
(@CatId, 'description', 'Additional Explanation (Optional)', 'textarea', NULL, 0, 1);

-- GENERIC DOCUMENT REQUEST
SELECT @CatId = [CategoryId] FROM [dbo].[Service_Categories] WHERE [CategoryName] = 'Residence Certificate';
INSERT INTO [dbo].[Service_Form_Schema] (CategoryId, FieldName, FieldLabel, FieldType, DropdownOptions, IsRequired, DisplayOrder) VALUES
(@CatId, 'applicantType', 'Whom is this request for?', 'applicant-type', NULL, 0, 1),
(@CatId, 'documentType', 'Document Type', 'dropdown', '[{"label":"Birth Certificate","value":"birth"},{"label":"Death Certificate","value":"death"},{"label":"Marriage Certificate","value":"marriage"},{"label":"Family Card","value":"family-card"},{"label":"Voter ID","value":"voter-id"},{"label":"Other","value":"other"}]', 1, 2),
(@CatId, 'subject', 'Brief Subject', 'text', NULL, 1, 3),
(@CatId, 'description', 'Detailed Description', 'textarea', NULL, 1, 4);
