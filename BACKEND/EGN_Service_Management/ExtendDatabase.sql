-- 1. Family Membership History
CREATE TABLE [dbo].[Family_Member_History] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [CitizenNic] NVARCHAR(20) NOT NULL,
    [FamilyCardNumber] NVARCHAR(50) NOT NULL,
    [JoinedDate] DATE,
    [LeftDate] DATE NULL,
    [Reason] NVARCHAR(MAX),
    CONSTRAINT [FK_Family_Member_History_Citizen_Master] FOREIGN KEY ([CitizenNic]) REFERENCES [dbo].[Citizen_Master]([Nic]),
    CONSTRAINT [FK_Family_Member_History_Family_Cards] FOREIGN KEY ([FamilyCardNumber]) REFERENCES [dbo].[family_cards]([FamilyCardNumber])
);

-- 2. Service Master Data
CREATE TABLE [dbo].[Services] (
    [ServiceId] INT IDENTITY(1,1) PRIMARY KEY,
    [ServiceName] NVARCHAR(255) NOT NULL
);

-- Insert Seed Data for Services
INSERT INTO [dbo].[Services] ([ServiceName]) VALUES 
('Family Card Services'),
('Register to New GN Division'),
('Voter Registration (18+)'),
('Document Request'),
('Document Verification'),
('Welfare & Assistance'),
('Complaints & Grievances'),
('Other Requests');

CREATE TABLE [dbo].[Service_Categories] (
    [CategoryId] INT IDENTITY(1,1) PRIMARY KEY,
    [ServiceId] INT NOT NULL,
    [CategoryName] NVARCHAR(255) NOT NULL,
    CONSTRAINT [FK_Service_Categories_Services] FOREIGN KEY ([ServiceId]) REFERENCES [dbo].[Services]([ServiceId]) ON DELETE CASCADE
);

-- Insert predefined categories
DECLARE @SvcId INT;

SELECT @SvcId = [ServiceId] FROM [dbo].[Services] WHERE [ServiceName] = 'Family Card Services';
INSERT INTO [dbo].[Service_Categories] ([ServiceId], [CategoryName]) VALUES
(@SvcId, 'Add Newborn to Family Card'),
(@SvcId, 'Add Family Member'),
(@SvcId, 'Remove Deceased Family Member'),
(@SvcId, 'Change Family Head'),
(@SvcId, 'Split Family Card by Marriage'),
(@SvcId, 'Remove Migrated Family Member');

SELECT @SvcId = [ServiceId] FROM [dbo].[Services] WHERE [ServiceName] = 'Voter Registration (18+)';
INSERT INTO [dbo].[Service_Categories] ([ServiceId], [CategoryName]) VALUES
(@SvcId, 'New Voter Registration'),
(@SvcId, 'Address Update for Voter List'),
(@SvcId, 'Correction of Voter Details');

SELECT @SvcId = [ServiceId] FROM [dbo].[Services] WHERE [ServiceName] = 'Document Request';
INSERT INTO [dbo].[Service_Categories] ([ServiceId], [CategoryName]) VALUES
(@SvcId, 'Residence Certificate'),
(@SvcId, 'Character Certificate'),
(@SvcId, 'Income / Social Status Confirmation'),
(@SvcId, 'Temporary Residence Certificate'),
(@SvcId, 'Death Proof'),
(@SvcId, 'Other Certificate Request');

SELECT @SvcId = [ServiceId] FROM [dbo].[Services] WHERE [ServiceName] = 'Document Verification';
INSERT INTO [dbo].[Service_Categories] ([ServiceId], [CategoryName]) VALUES
(@SvcId, 'NIC Details Verification'),
(@SvcId, 'Residence Proof Verification'),
(@SvcId, 'Annual income'),
(@SvcId, 'Government Form Verification'),
(@SvcId, 'Other Document Authentication');

SELECT @SvcId = [ServiceId] FROM [dbo].[Services] WHERE [ServiceName] = 'Welfare & Assistance';
INSERT INTO [dbo].[Service_Categories] ([ServiceId], [CategoryName]) VALUES
(@SvcId, 'Aswesuma Welfare Benefit Scheme'),
(@SvcId, 'Public Assistance Programme'),
(@SvcId, 'Elderly Allowance'),
(@SvcId, 'Disability Allowance'),
(@SvcId, 'Kidney Disease Allowance'),
(@SvcId, 'Pregnant Mother Nutritional Assistance (Poshana Malla)'),
(@SvcId, 'Housing Assistance Programmes');

SELECT @SvcId = [ServiceId] FROM [dbo].[Services] WHERE [ServiceName] = 'Complaints & Grievances';
INSERT INTO [dbo].[Service_Categories] ([ServiceId], [CategoryName]) VALUES
(@SvcId, 'Incorrect Record Complaint (Family Card / Address)'),
(@SvcId, 'Document Issuance Delay Complaint'),
(@SvcId, 'Welfare Application Issue Complaint'),
(@SvcId, 'Land / Address Dispute Complaint'),
(@SvcId, 'Community / Environmental Issues'),
(@SvcId, 'Infrastructure Issues'),
(@SvcId, 'Fraud / Misuse Report'),
(@SvcId, 'Other Complaint');

SELECT @SvcId = [ServiceId] FROM [dbo].[Services] WHERE [ServiceName] = 'Other Requests';
INSERT INTO [dbo].[Service_Categories] ([ServiceId], [CategoryName]) VALUES
(@SvcId, 'General Information Request'),
(@SvcId, 'Certificate Clarification Request'),
(@SvcId, 'Correction Request (Non-specific Records)'),
(@SvcId, 'Special Case Request'),
(@SvcId, 'Emergency Assistance Request'),
(@SvcId, 'Miscellaneous Administrative Request');


-- 3. Unified Request Engine
CREATE TABLE [dbo].[Requests] (
    [RequestId] INT IDENTITY(1,1) PRIMARY KEY,
    [CitizenNic] NVARCHAR(20) NOT NULL,
    [ServiceId] INT NOT NULL,
    [CategoryId] INT NOT NULL,
    [CurrentStatus] NVARCHAR(50) NOT NULL, -- Submitted, InProgress, Verified, Approved, Rejected
    [SubmittedDate] DATETIME2 NOT NULL,
    [LastUpdated] DATETIME2 NOT NULL,
    [AssignedGnDivisionNic] NVARCHAR(20) NULL,
    [ApprovedDate] DATETIME2 NULL,
    [RejectedDate] DATETIME2 NULL,
    CONSTRAINT [FK_Requests_Citizen_Master] FOREIGN KEY ([CitizenNic]) REFERENCES [dbo].[Citizen_Master]([Nic]),
    CONSTRAINT [FK_Requests_Services] FOREIGN KEY ([ServiceId]) REFERENCES [dbo].[Services]([ServiceId]),
    CONSTRAINT [FK_Requests_Categories] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Service_Categories]([CategoryId]),
    CONSTRAINT [FK_Requests_GnOfficers] FOREIGN KEY ([AssignedGnDivisionNic]) REFERENCES [dbo].[gn_officers]([Nic])
);

-- 4. Dynamic Form Storage
CREATE TABLE [dbo].[Request_Data] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [RequestId] INT NOT NULL,
    [JsonData] NVARCHAR(MAX) NULL,
    [CreatedDate] DATETIME2 NOT NULL,
    CONSTRAINT [FK_Request_Data_Requests] FOREIGN KEY ([RequestId]) REFERENCES [dbo].[Requests]([RequestId]) ON DELETE CASCADE
);

-- 5. Attachments
CREATE TABLE [dbo].[Request_Attachments] (
    [AttachmentId] INT IDENTITY(1,1) PRIMARY KEY,
    [RequestId] INT NOT NULL,
    [FileName] NVARCHAR(500) NOT NULL,
    [FileUrl] NVARCHAR(MAX) NOT NULL,
    [FileType] NVARCHAR(100) NULL,
    [UploadedAt] DATETIME2 NOT NULL,
    CONSTRAINT [FK_Request_Attachments_Requests] FOREIGN KEY ([RequestId]) REFERENCES [dbo].[Requests]([RequestId]) ON DELETE CASCADE
);

-- 6. Status Audit History
CREATE TABLE [dbo].[Request_Status_History] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [RequestId] INT NOT NULL,
    [Status] NVARCHAR(50) NOT NULL,
    [Remarks] NVARCHAR(MAX) NULL,
    [ChangedBy] NVARCHAR(20) NOT NULL,
    [ChangedDate] DATETIME2 NOT NULL,
    CONSTRAINT [FK_Request_Status_History_Requests] FOREIGN KEY ([RequestId]) REFERENCES [dbo].[Requests]([RequestId]) ON DELETE CASCADE,
    CONSTRAINT [FK_Request_Status_History_Accounts] FOREIGN KEY ([ChangedBy]) REFERENCES [dbo].[accounts]([Nic])
);

-- 7. Officer Remarks
CREATE TABLE [dbo].[Request_Remarks] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [RequestId] INT NOT NULL,
    [OfficerNic] NVARCHAR(20) NOT NULL,
    [Remark] NVARCHAR(MAX) NOT NULL,
    [CreatedDate] DATETIME2 NOT NULL,
    CONSTRAINT [FK_Request_Remarks_Requests] FOREIGN KEY ([RequestId]) REFERENCES [dbo].[Requests]([RequestId]) ON DELETE CASCADE,
    CONSTRAINT [FK_Request_Remarks_GnOfficers] FOREIGN KEY ([OfficerNic]) REFERENCES [dbo].[gn_officers]([Nic])
);

-- 8. Welfare Verification Table
CREATE TABLE [dbo].[Citizen_Welfare_Status] (
    [CitizenNic] NVARCHAR(20) PRIMARY KEY,
    [IsAswesuma] BIT NOT NULL DEFAULT 0,
    [IsDisabilityEligible] BIT NOT NULL DEFAULT 0,
    [IsElderlyEligible] BIT NOT NULL DEFAULT 0,
    [IsKidneyPatient] BIT NOT NULL DEFAULT 0,
    [IsPregnantMotherEligible] BIT NOT NULL DEFAULT 0,
    [LastVerifiedDate] DATETIME2 NULL,
    [VerifiedByGnOfficer] NVARCHAR(20) NULL,
    CONSTRAINT [FK_Citizen_Welfare_Status_Citizen_Master] FOREIGN KEY ([CitizenNic]) REFERENCES [dbo].[Citizen_Master]([Nic]),
    CONSTRAINT [FK_Citizen_Welfare_Status_GnOfficers] FOREIGN KEY ([VerifiedByGnOfficer]) REFERENCES [dbo].[gn_officers]([Nic])
);
