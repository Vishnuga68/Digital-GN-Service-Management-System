-- Alter Request_Attachments table to add new columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Request_Attachments]') AND name = N'DocumentRole')
BEGIN
    ALTER TABLE [Request_Attachments] ADD [DocumentRole] nvarchar(50) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Request_Attachments]') AND name = N'DisplayName')
BEGIN
    ALTER TABLE [Request_Attachments] ADD [DisplayName] nvarchar(255) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Request_Attachments]') AND name = N'IsPrimary')
BEGIN
    ALTER TABLE [Request_Attachments] ADD [IsPrimary] bit NOT NULL DEFAULT 0;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Request_Attachments]') AND name = N'UploadedBy')
BEGIN
    ALTER TABLE [Request_Attachments] ADD [UploadedBy] nvarchar(100) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Request_Attachments]') AND name = N'FileSize')
BEGIN
    ALTER TABLE [Request_Attachments] ADD [FileSize] bigint NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Request_Attachments]') AND name = N'MimeType')
BEGIN
    ALTER TABLE [Request_Attachments] ADD [MimeType] nvarchar(100) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Request_Attachments]') AND name = N'IsActive')
BEGIN
    ALTER TABLE [Request_Attachments] ADD [IsActive] bit NOT NULL DEFAULT 1;
END

-- Create Document_Verifications table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Document_Verifications]') AND type in (N'U'))
BEGIN
    CREATE TABLE [Document_Verifications] (
        [VerificationId] int IDENTITY(1,1) NOT NULL,
        [RequestId] int NOT NULL,
        [MainAttachmentId] int NOT NULL,
        [AssignedGNOfficerNIC] nvarchar(20) NULL,
        [VerificationStatus] nvarchar(50) NOT NULL,
        [VerificationRemarks] nvarchar(max) NULL,
        [VerifiedDate] datetime2 NULL,
        [FinalVerifiedAttachmentId] int NULL,
        [VerificationTokenHash] nvarchar(255) NULL,
        [VerificationUrl] nvarchar(500) NULL,
        [OriginalFileHash] nvarchar(255) NULL,
        [FinalFileHash] nvarchar(255) NULL,
        [CreatedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedDate] datetime2 NULL,
        [RevocationReason] nvarchar(max) NULL,
        [RevokedDate] datetime2 NULL,
        [RevokedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Document_Verifications] PRIMARY KEY ([VerificationId]),
        CONSTRAINT [FK_Document_Verifications_Requests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [Requests] ([RequestId]) ON DELETE CASCADE,
        CONSTRAINT [FK_Document_Verifications_Request_Attachments_MainAttachmentId] FOREIGN KEY ([MainAttachmentId]) REFERENCES [Request_Attachments] ([AttachmentId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Document_Verifications_Request_Attachments_FinalVerifiedAttachmentId] FOREIGN KEY ([FinalVerifiedAttachmentId]) REFERENCES [Request_Attachments] ([AttachmentId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Document_Verifications_gn_officers_AssignedGNOfficerNIC] FOREIGN KEY ([AssignedGNOfficerNIC]) REFERENCES [gn_officers] ([Nic]) ON DELETE NO ACTION
    );

    CREATE UNIQUE INDEX [IX_Document_Verifications_RequestId] ON [Document_Verifications] ([RequestId]);
END
GO
