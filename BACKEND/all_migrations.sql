IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [accounts] (
        [Nic] nvarchar(20) NOT NULL,
        [PasswordHash] nvarchar(255) NOT NULL,
        [Role] nvarchar(30) NOT NULL,
        [Email] nvarchar(100) NULL,
        [Phone] nvarchar(20) NULL,
        [Status] nvarchar(20) NOT NULL,
        [LastLogin] datetime2 NULL,
        CONSTRAINT [PK_accounts] PRIMARY KEY ([Nic])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [document_categories] (
        [Id] nvarchar(50) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_document_categories] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [ds_divisions] (
        [Id] nvarchar(50) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [District] nvarchar(100) NOT NULL,
        [Province] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_ds_divisions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [notifications] (
        [Id] int NOT NULL IDENTITY,
        [RecipientNic] nvarchar(20) NOT NULL,
        [Type] nvarchar(30) NOT NULL,
        [Title] nvarchar(255) NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        [IsRead] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_notifications_accounts_RecipientNic] FOREIGN KEY ([RecipientNic]) REFERENCES [accounts] ([Nic]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [ds_officers] (
        [Nic] nvarchar(20) NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [DsDivisionId] nvarchar(50) NULL,
        CONSTRAINT [PK_ds_officers] PRIMARY KEY ([Nic]),
        CONSTRAINT [FK_ds_officers_accounts_Nic] FOREIGN KEY ([Nic]) REFERENCES [accounts] ([Nic]) ON DELETE CASCADE,
        CONSTRAINT [FK_ds_officers_ds_divisions_DsDivisionId] FOREIGN KEY ([DsDivisionId]) REFERENCES [ds_divisions] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [gn_divisions] (
        [Id] nvarchar(50) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [DsDivisionId] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_gn_divisions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_gn_divisions_ds_divisions_DsDivisionId] FOREIGN KEY ([DsDivisionId]) REFERENCES [ds_divisions] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [gn_officers] (
        [Nic] nvarchar(20) NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [GnDivisionId] nvarchar(50) NULL,
        CONSTRAINT [PK_gn_officers] PRIMARY KEY ([Nic]),
        CONSTRAINT [FK_gn_officers_accounts_Nic] FOREIGN KEY ([Nic]) REFERENCES [accounts] ([Nic]) ON DELETE CASCADE,
        CONSTRAINT [FK_gn_officers_gn_divisions_GnDivisionId] FOREIGN KEY ([GnDivisionId]) REFERENCES [gn_divisions] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [announcements] (
        [Id] nvarchar(50) NOT NULL,
        [GnOfficerNic] nvarchar(20) NOT NULL,
        [Type] nvarchar(30) NOT NULL,
        [Title] nvarchar(255) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Priority] bit NOT NULL,
        [ActionLabel] nvarchar(100) NULL,
        [ActionLink] nvarchar(255) NULL,
        [AttachmentName] nvarchar(255) NULL,
        [Status] nvarchar(30) NOT NULL,
        [EstimatedTime] nvarchar(20) NULL,
        [SurveyLink] nvarchar(255) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_announcements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_announcements_gn_officers_GnOfficerNic] FOREIGN KEY ([GnOfficerNic]) REFERENCES [gn_officers] ([Nic]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [download_forms] (
        [Id] nvarchar(50) NOT NULL,
        [Title] nvarchar(255) NOT NULL,
        [CategoryId] nvarchar(50) NOT NULL,
        [FileType] nvarchar(50) NOT NULL,
        [FileSize] nvarchar(50) NOT NULL,
        [FileUrl] nvarchar(255) NOT NULL,
        [DownloadsCount] int NOT NULL,
        [IsFeatured] bit NOT NULL,
        [UploadedByNic] nvarchar(20) NULL,
        [UploadedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_download_forms] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_download_forms_document_categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [document_categories] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_download_forms_gn_officers_UploadedByNic] FOREIGN KEY ([UploadedByNic]) REFERENCES [gn_officers] ([Nic]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [gn_leaves_holidays] (
        [Id] int NOT NULL IDENTITY,
        [GnOfficerNic] nvarchar(20) NOT NULL,
        [EventDate] date NOT NULL,
        [Summary] nvarchar(255) NOT NULL,
        [EventType] nvarchar(30) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_gn_leaves_holidays] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_gn_leaves_holidays_gn_officers_GnOfficerNic] FOREIGN KEY ([GnOfficerNic]) REFERENCES [gn_officers] ([Nic]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [transfer_requests] (
        [Id] nvarchar(50) NOT NULL,
        [GnOfficerNic] nvarchar(20) NOT NULL,
        [CurrentDivisionId] nvarchar(50) NOT NULL,
        [RequestedDivisionId] nvarchar(50) NOT NULL,
        [Reason] nvarchar(max) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [DocumentName] nvarchar(255) NULL,
        [DocumentUrl] nvarchar(255) NULL,
        [SubmittedDate] date NOT NULL,
        [ReviewedByNic] nvarchar(20) NULL,
        [ReviewedDate] date NULL,
        CONSTRAINT [PK_transfer_requests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_transfer_requests_ds_officers_ReviewedByNic] FOREIGN KEY ([ReviewedByNic]) REFERENCES [ds_officers] ([Nic]) ON DELETE SET NULL,
        CONSTRAINT [FK_transfer_requests_gn_divisions_CurrentDivisionId] FOREIGN KEY ([CurrentDivisionId]) REFERENCES [gn_divisions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_transfer_requests_gn_divisions_RequestedDivisionId] FOREIGN KEY ([RequestedDivisionId]) REFERENCES [gn_divisions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_transfer_requests_gn_officers_GnOfficerNic] FOREIGN KEY ([GnOfficerNic]) REFERENCES [gn_officers] ([Nic]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [poll_options] (
        [Id] nvarchar(50) NOT NULL,
        [AnnouncementId] nvarchar(50) NOT NULL,
        [OptionText] nvarchar(255) NOT NULL,
        CONSTRAINT [PK_poll_options] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_poll_options_announcements_AnnouncementId] FOREIGN KEY ([AnnouncementId]) REFERENCES [announcements] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [announcement_target_citizens] (
        [Id] int NOT NULL IDENTITY,
        [AnnouncementId] nvarchar(50) NOT NULL,
        [CitizenNic] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_announcement_target_citizens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_announcement_target_citizens_announcements_AnnouncementId] FOREIGN KEY ([AnnouncementId]) REFERENCES [announcements] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [appointments] (
        [Id] nvarchar(50) NOT NULL,
        [CitizenNic] nvarchar(20) NOT NULL,
        [GnOfficerNic] nvarchar(20) NOT NULL,
        [AppointmentDate] date NOT NULL,
        [TimeSlot] nvarchar(50) NOT NULL,
        [Service] nvarchar(100) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_appointments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_appointments_gn_officers_GnOfficerNic] FOREIGN KEY ([GnOfficerNic]) REFERENCES [gn_officers] ([Nic]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [chat_attachments] (
        [Id] nvarchar(50) NOT NULL,
        [MessageId] nvarchar(50) NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [FileType] nvarchar(50) NOT NULL,
        [FileUrl] nvarchar(255) NOT NULL,
        CONSTRAINT [PK_chat_attachments] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [chat_messages] (
        [Id] nvarchar(50) NOT NULL,
        [ConversationId] nvarchar(50) NOT NULL,
        [SenderNic] nvarchar(20) NOT NULL,
        [MessageText] nvarchar(max) NOT NULL,
        [IsRead] bit NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_chat_messages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_chat_messages_accounts_SenderNic] FOREIGN KEY ([SenderNic]) REFERENCES [accounts] ([Nic]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [Citizen_Master] (
        [Nic] nvarchar(20) NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [Dob] date NOT NULL,
        [Gender] nvarchar(20) NOT NULL,
        [MaritalStatus] nvarchar(20) NOT NULL,
        [Religion] nvarchar(50) NULL,
        [Occupation] nvarchar(100) NULL,
        [FamilyCardNumber] nvarchar(50) NULL,
        [RelationshipToHead] nvarchar(50) NULL,
        [LifeStatus] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_Citizen_Master] PRIMARY KEY ([Nic])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [citizen_special_statuses] (
        [CitizenNic] nvarchar(20) NOT NULL,
        [IsNalanpuri] bit NOT NULL,
        [HasKidneyDisease] bit NOT NULL,
        [HasCancer] bit NOT NULL,
        [HasOtherDisability] bit NOT NULL,
        [WelfareDetails] nvarchar(max) NULL,
        CONSTRAINT [PK_citizen_special_statuses] PRIMARY KEY ([CitizenNic]),
        CONSTRAINT [FK_citizen_special_statuses_Citizen_Master_CitizenNic] FOREIGN KEY ([CitizenNic]) REFERENCES [Citizen_Master] ([Nic]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [EGN_Service_User] (
        [Nic] nvarchar(20) NOT NULL,
        [Username] nvarchar(100) NOT NULL,
        [RegisteredAt] datetime2 NOT NULL,
        CONSTRAINT [PK_EGN_Service_User] PRIMARY KEY ([Nic]),
        CONSTRAINT [FK_EGN_Service_User_Citizen_Master_Nic] FOREIGN KEY ([Nic]) REFERENCES [Citizen_Master] ([Nic]) ON DELETE CASCADE,
        CONSTRAINT [FK_EGN_Service_User_accounts_Nic] FOREIGN KEY ([Nic]) REFERENCES [accounts] ([Nic]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [family_cards] (
        [FamilyCardNumber] nvarchar(50) NOT NULL,
        [HeadOfHouseholdNic] nvarchar(20) NOT NULL,
        [GnDivisionId] nvarchar(50) NOT NULL,
        [Address] nvarchar(max) NOT NULL,
        [RegisteredDate] date NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_family_cards] PRIMARY KEY ([FamilyCardNumber]),
        CONSTRAINT [FK_family_cards_Citizen_Master_HeadOfHouseholdNic] FOREIGN KEY ([HeadOfHouseholdNic]) REFERENCES [Citizen_Master] ([Nic]) ON DELETE NO ACTION,
        CONSTRAINT [FK_family_cards_gn_divisions_GnDivisionId] FOREIGN KEY ([GnDivisionId]) REFERENCES [gn_divisions] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [service_requests] (
        [Id] nvarchar(50) NOT NULL,
        [CitizenNic] nvarchar(20) NOT NULL,
        [GnDivisionId] nvarchar(50) NOT NULL,
        [ServiceType] nvarchar(100) NOT NULL,
        [Category] nvarchar(50) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [CurrentStep] int NOT NULL,
        [RejectionReason] nvarchar(max) NULL,
        [FinalDocumentUrl] nvarchar(255) NULL,
        [SubmittedDate] datetime2 NOT NULL,
        [LastUpdated] datetime2 NOT NULL,
        CONSTRAINT [PK_service_requests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_service_requests_Citizen_Master_CitizenNic] FOREIGN KEY ([CitizenNic]) REFERENCES [Citizen_Master] ([Nic]) ON DELETE NO ACTION,
        CONSTRAINT [FK_service_requests_gn_divisions_GnDivisionId] FOREIGN KEY ([GnDivisionId]) REFERENCES [gn_divisions] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [poll_votes] (
        [Id] int NOT NULL IDENTITY,
        [AnnouncementId] nvarchar(50) NOT NULL,
        [CitizenNic] nvarchar(20) NOT NULL,
        [PollOptionId] nvarchar(50) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_poll_votes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_poll_votes_EGN_Service_User_CitizenNic] FOREIGN KEY ([CitizenNic]) REFERENCES [EGN_Service_User] ([Nic]) ON DELETE CASCADE,
        CONSTRAINT [FK_poll_votes_announcements_AnnouncementId] FOREIGN KEY ([AnnouncementId]) REFERENCES [announcements] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_poll_votes_poll_options_PollOptionId] FOREIGN KEY ([PollOptionId]) REFERENCES [poll_options] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [survey_responses] (
        [Id] int NOT NULL IDENTITY,
        [AnnouncementId] nvarchar(50) NOT NULL,
        [CitizenNic] nvarchar(20) NOT NULL,
        [ResponseText] nvarchar(max) NOT NULL,
        [SubmittedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_survey_responses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_survey_responses_EGN_Service_User_CitizenNic] FOREIGN KEY ([CitizenNic]) REFERENCES [EGN_Service_User] ([Nic]) ON DELETE CASCADE,
        CONSTRAINT [FK_survey_responses_announcements_AnnouncementId] FOREIGN KEY ([AnnouncementId]) REFERENCES [announcements] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [conversations] (
        [Id] nvarchar(50) NOT NULL,
        [CitizenNic] nvarchar(20) NOT NULL,
        [GnOfficerNic] nvarchar(20) NOT NULL,
        [RequestId] nvarchar(50) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_conversations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_conversations_EGN_Service_User_CitizenNic] FOREIGN KEY ([CitizenNic]) REFERENCES [EGN_Service_User] ([Nic]) ON DELETE CASCADE,
        CONSTRAINT [FK_conversations_gn_officers_GnOfficerNic] FOREIGN KEY ([GnOfficerNic]) REFERENCES [gn_officers] ([Nic]) ON DELETE NO ACTION,
        CONSTRAINT [FK_conversations_service_requests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [service_requests] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [request_attachments] (
        [Id] nvarchar(50) NOT NULL,
        [RequestId] nvarchar(50) NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [FileType] nvarchar(50) NOT NULL,
        [FileSize] nvarchar(20) NOT NULL,
        [UploadedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_request_attachments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_request_attachments_service_requests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [service_requests] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [request_remarks] (
        [Id] int NOT NULL IDENTITY,
        [RequestId] nvarchar(50) NOT NULL,
        [AuthorNic] nvarchar(20) NOT NULL,
        [RoleLabel] nvarchar(50) NOT NULL,
        [RemarkText] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_request_remarks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_request_remarks_accounts_AuthorNic] FOREIGN KEY ([AuthorNic]) REFERENCES [accounts] ([Nic]) ON DELETE NO ACTION,
        CONSTRAINT [FK_request_remarks_service_requests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [service_requests] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE TABLE [request_timelines] (
        [Id] int NOT NULL IDENTITY,
        [RequestId] nvarchar(50) NOT NULL,
        [Step] int NOT NULL,
        [Title] nvarchar(100) NOT NULL,
        [Timestamp] datetime2 NULL,
        [Completed] bit NOT NULL,
        [OfficerNic] nvarchar(20) NULL,
        CONSTRAINT [PK_request_timelines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_request_timelines_accounts_OfficerNic] FOREIGN KEY ([OfficerNic]) REFERENCES [accounts] ([Nic]) ON DELETE SET NULL,
        CONSTRAINT [FK_request_timelines_service_requests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [service_requests] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_accounts_Email] ON [accounts] ([Email]) WHERE [Email] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE UNIQUE INDEX [IX_announcement_target_citizens_AnnouncementId_CitizenNic] ON [announcement_target_citizens] ([AnnouncementId], [CitizenNic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_announcement_target_citizens_CitizenNic] ON [announcement_target_citizens] ([CitizenNic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_announcements_GnOfficerNic] ON [announcements] ([GnOfficerNic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_appointments_CitizenNic] ON [appointments] ([CitizenNic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_appointments_GnOfficerNic] ON [appointments] ([GnOfficerNic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_chat_attachments_MessageId] ON [chat_attachments] ([MessageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_chat_messages_ConversationId] ON [chat_messages] ([ConversationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_chat_messages_SenderNic] ON [chat_messages] ([SenderNic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_Citizen_Master_FamilyCardNumber] ON [Citizen_Master] ([FamilyCardNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_conversations_CitizenNic] ON [conversations] ([CitizenNic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_conversations_GnOfficerNic] ON [conversations] ([GnOfficerNic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_conversations_RequestId] ON [conversations] ([RequestId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE UNIQUE INDEX [IX_document_categories_Name] ON [document_categories] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_download_forms_CategoryId] ON [download_forms] ([CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_download_forms_UploadedByNic] ON [download_forms] ([UploadedByNic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ds_divisions_Name] ON [ds_divisions] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_ds_officers_DsDivisionId] ON [ds_officers] ([DsDivisionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EGN_Service_User_Username] ON [EGN_Service_User] ([Username]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_family_cards_GnDivisionId] ON [family_cards] ([GnDivisionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_family_cards_HeadOfHouseholdNic] ON [family_cards] ([HeadOfHouseholdNic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_gn_divisions_DsDivisionId] ON [gn_divisions] ([DsDivisionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE UNIQUE INDEX [IX_gn_divisions_Name_DsDivisionId] ON [gn_divisions] ([Name], [DsDivisionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_gn_leaves_holidays_GnOfficerNic] ON [gn_leaves_holidays] ([GnOfficerNic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_gn_officers_GnDivisionId] ON [gn_officers] ([GnDivisionId]) WHERE [GnDivisionId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_notifications_RecipientNic] ON [notifications] ([RecipientNic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_poll_options_AnnouncementId] ON [poll_options] ([AnnouncementId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE UNIQUE INDEX [IX_poll_votes_AnnouncementId_CitizenNic] ON [poll_votes] ([AnnouncementId], [CitizenNic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_poll_votes_CitizenNic] ON [poll_votes] ([CitizenNic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_poll_votes_PollOptionId] ON [poll_votes] ([PollOptionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_request_attachments_RequestId] ON [request_attachments] ([RequestId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_request_remarks_AuthorNic] ON [request_remarks] ([AuthorNic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_request_remarks_RequestId] ON [request_remarks] ([RequestId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_request_timelines_OfficerNic] ON [request_timelines] ([OfficerNic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_request_timelines_RequestId] ON [request_timelines] ([RequestId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_service_requests_CitizenNic] ON [service_requests] ([CitizenNic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_service_requests_GnDivisionId] ON [service_requests] ([GnDivisionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_survey_responses_AnnouncementId] ON [survey_responses] ([AnnouncementId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_survey_responses_CitizenNic] ON [survey_responses] ([CitizenNic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_transfer_requests_CurrentDivisionId] ON [transfer_requests] ([CurrentDivisionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_transfer_requests_GnOfficerNic] ON [transfer_requests] ([GnOfficerNic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_transfer_requests_RequestedDivisionId] ON [transfer_requests] ([RequestedDivisionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    CREATE INDEX [IX_transfer_requests_ReviewedByNic] ON [transfer_requests] ([ReviewedByNic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    ALTER TABLE [announcement_target_citizens] ADD CONSTRAINT [FK_announcement_target_citizens_Citizen_Master_CitizenNic] FOREIGN KEY ([CitizenNic]) REFERENCES [Citizen_Master] ([Nic]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    ALTER TABLE [appointments] ADD CONSTRAINT [FK_appointments_EGN_Service_User_CitizenNic] FOREIGN KEY ([CitizenNic]) REFERENCES [EGN_Service_User] ([Nic]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    ALTER TABLE [chat_attachments] ADD CONSTRAINT [FK_chat_attachments_chat_messages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [chat_messages] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    ALTER TABLE [chat_messages] ADD CONSTRAINT [FK_chat_messages_conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [conversations] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    ALTER TABLE [Citizen_Master] ADD CONSTRAINT [FK_Citizen_Master_family_cards_FamilyCardNumber] FOREIGN KEY ([FamilyCardNumber]) REFERENCES [family_cards] ([FamilyCardNumber]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260526094714_test'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260526094714_test', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260529050505_AddGnDivisionAssignment'
)
BEGIN
    CREATE TABLE [gn_division_assignments] (
        [Id] int NOT NULL IDENTITY,
        [GnOfficerNic] nvarchar(20) NOT NULL,
        [GnDivisionId] nvarchar(50) NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NULL,
        [Status] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_gn_division_assignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_gn_division_assignments_gn_divisions_GnDivisionId] FOREIGN KEY ([GnDivisionId]) REFERENCES [gn_divisions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_gn_division_assignments_gn_officers_GnOfficerNic] FOREIGN KEY ([GnOfficerNic]) REFERENCES [gn_officers] ([Nic]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260529050505_AddGnDivisionAssignment'
)
BEGIN
    CREATE INDEX [IX_gn_division_assignments_GnDivisionId] ON [gn_division_assignments] ([GnDivisionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260529050505_AddGnDivisionAssignment'
)
BEGIN
    CREATE INDEX [IX_gn_division_assignments_GnOfficerNic] ON [gn_division_assignments] ([GnOfficerNic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260529050505_AddGnDivisionAssignment'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260529050505_AddGnDivisionAssignment', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260529062948_AddGnDivisionToCitizenMaster'
)
BEGIN
    ALTER TABLE [Citizen_Master] ADD [GnDivisionId] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260529062948_AddGnDivisionToCitizenMaster'
)
BEGIN
    CREATE INDEX [IX_Citizen_Master_GnDivisionId] ON [Citizen_Master] ([GnDivisionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260529062948_AddGnDivisionToCitizenMaster'
)
BEGIN
    ALTER TABLE [Citizen_Master] ADD CONSTRAINT [FK_Citizen_Master_gn_divisions_GnDivisionId] FOREIGN KEY ([GnDivisionId]) REFERENCES [gn_divisions] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260529062948_AddGnDivisionToCitizenMaster'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260529062948_AddGnDivisionToCitizenMaster', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260605154118_RemoveRoleFromAccounts'
)
BEGIN
    ALTER TABLE [appointments] DROP CONSTRAINT [FK_appointments_EGN_Service_User_CitizenNic];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260605154118_RemoveRoleFromAccounts'
)
BEGIN
    ALTER TABLE [conversations] DROP CONSTRAINT [FK_conversations_EGN_Service_User_CitizenNic];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260605154118_RemoveRoleFromAccounts'
)
BEGIN
    ALTER TABLE [poll_votes] DROP CONSTRAINT [FK_poll_votes_EGN_Service_User_CitizenNic];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260605154118_RemoveRoleFromAccounts'
)
BEGIN
    ALTER TABLE [survey_responses] DROP CONSTRAINT [FK_survey_responses_EGN_Service_User_CitizenNic];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260605154118_RemoveRoleFromAccounts'
)
BEGIN
    DROP TABLE [EGN_Service_User];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260605154118_RemoveRoleFromAccounts'
)
BEGIN
    CREATE TABLE [EGN_Service_Users] (
        [Nic] nvarchar(20) NOT NULL,
        [RegisteredAt] datetime2 NOT NULL,
        CONSTRAINT [PK_EGN_Service_Users] PRIMARY KEY ([Nic]),
        CONSTRAINT [FK_EGN_Service_Users_Citizen_Master_Nic] FOREIGN KEY ([Nic]) REFERENCES [Citizen_Master] ([Nic]) ON DELETE CASCADE,
        CONSTRAINT [FK_EGN_Service_Users_accounts_Nic] FOREIGN KEY ([Nic]) REFERENCES [accounts] ([Nic]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260605154118_RemoveRoleFromAccounts'
)
BEGIN
    ALTER TABLE [appointments] ADD CONSTRAINT [FK_appointments_EGN_Service_Users_CitizenNic] FOREIGN KEY ([CitizenNic]) REFERENCES [EGN_Service_Users] ([Nic]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260605154118_RemoveRoleFromAccounts'
)
BEGIN
    ALTER TABLE [conversations] ADD CONSTRAINT [FK_conversations_EGN_Service_Users_CitizenNic] FOREIGN KEY ([CitizenNic]) REFERENCES [EGN_Service_Users] ([Nic]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260605154118_RemoveRoleFromAccounts'
)
BEGIN
    ALTER TABLE [poll_votes] ADD CONSTRAINT [FK_poll_votes_EGN_Service_Users_CitizenNic] FOREIGN KEY ([CitizenNic]) REFERENCES [EGN_Service_Users] ([Nic]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260605154118_RemoveRoleFromAccounts'
)
BEGIN
    ALTER TABLE [survey_responses] ADD CONSTRAINT [FK_survey_responses_EGN_Service_Users_CitizenNic] FOREIGN KEY ([CitizenNic]) REFERENCES [EGN_Service_Users] ([Nic]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260605154118_RemoveRoleFromAccounts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260605154118_RemoveRoleFromAccounts', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260605181715_RenameHasOtherDisabilityToHasElderlyEligible'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Citizen_Master]') AND [c].[name] = N'RelationshipToHead');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Citizen_Master] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Citizen_Master] DROP COLUMN [RelationshipToHead];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260605181715_RenameHasOtherDisabilityToHasElderlyEligible'
)
BEGIN
    EXEC sp_rename N'[citizen_special_statuses].[HasOtherDisability]', N'HasVoterId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260605181715_RenameHasOtherDisabilityToHasElderlyEligible'
)
BEGIN
    ALTER TABLE [citizen_special_statuses] ADD [HasElderlyEligible] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260605181715_RenameHasOtherDisabilityToHasElderlyEligible'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260605181715_RenameHasOtherDisabilityToHasElderlyEligible', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614170312_AddProvinceDistrictMaster'
)
BEGIN
    CREATE TABLE [Provinces] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Provinces] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614170312_AddProvinceDistrictMaster'
)
BEGIN
    CREATE TABLE [Districts] (
        [Id] int NOT NULL IDENTITY,
        [ProvinceId] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Districts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Districts_Provinces_ProvinceId] FOREIGN KEY ([ProvinceId]) REFERENCES [Provinces] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614170312_AddProvinceDistrictMaster'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Provinces]'))
        SET IDENTITY_INSERT [Provinces] ON;
    EXEC(N'INSERT INTO [Provinces] ([Id], [Name])
    VALUES (1, N''Northern''),
    (2, N''Eastern''),
    (3, N''North Western''),
    (4, N''North Central''),
    (5, N''Central''),
    (6, N''Western''),
    (7, N''Southern''),
    (8, N''Uva''),
    (9, N''Sabaragamuwa'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Provinces]'))
        SET IDENTITY_INSERT [Provinces] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614170312_AddProvinceDistrictMaster'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name', N'ProvinceId') AND [object_id] = OBJECT_ID(N'[Districts]'))
        SET IDENTITY_INSERT [Districts] ON;
    EXEC(N'INSERT INTO [Districts] ([Id], [Name], [ProvinceId])
    VALUES (1, N''Jaffna'', 1),
    (2, N''Kilinochchi'', 1),
    (3, N''Mannar'', 1),
    (4, N''Mullaitivu'', 1),
    (5, N''Vavuniya'', 1),
    (6, N''Trincomalee'', 2),
    (7, N''Batticaloa'', 2),
    (8, N''Ampara'', 2),
    (9, N''Kurunegala'', 3),
    (10, N''Puttalam'', 3),
    (11, N''Anuradhapura'', 4),
    (12, N''Polonnaruwa'', 4),
    (13, N''Kandy'', 5),
    (14, N''Matale'', 5),
    (15, N''Nuwara Eliya'', 5),
    (16, N''Colombo'', 6),
    (17, N''Gampaha'', 6),
    (18, N''Kalutara'', 6),
    (19, N''Galle'', 7),
    (20, N''Matara'', 7),
    (21, N''Hambantota'', 7),
    (22, N''Badulla'', 8),
    (23, N''Monaragala'', 8),
    (24, N''Ratnapura'', 9),
    (25, N''Kegalle'', 9)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name', N'ProvinceId') AND [object_id] = OBJECT_ID(N'[Districts]'))
        SET IDENTITY_INSERT [Districts] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614170312_AddProvinceDistrictMaster'
)
BEGIN
    ALTER TABLE [ds_divisions] ADD [DistrictId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614170312_AddProvinceDistrictMaster'
)
BEGIN
    ALTER TABLE [ds_divisions] ADD [ProvinceId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614170312_AddProvinceDistrictMaster'
)
BEGIN
    CREATE INDEX [IX_Districts_ProvinceId] ON [Districts] ([ProvinceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614170312_AddProvinceDistrictMaster'
)
BEGIN
    CREATE INDEX [IX_ds_divisions_DistrictId] ON [ds_divisions] ([DistrictId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614170312_AddProvinceDistrictMaster'
)
BEGIN
    CREATE INDEX [IX_ds_divisions_ProvinceId] ON [ds_divisions] ([ProvinceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614170312_AddProvinceDistrictMaster'
)
BEGIN
    ALTER TABLE [ds_divisions] ADD CONSTRAINT [FK_ds_divisions_Districts_DistrictId] FOREIGN KEY ([DistrictId]) REFERENCES [Districts] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614170312_AddProvinceDistrictMaster'
)
BEGIN
    ALTER TABLE [ds_divisions] ADD CONSTRAINT [FK_ds_divisions_Provinces_ProvinceId] FOREIGN KEY ([ProvinceId]) REFERENCES [Provinces] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614170312_AddProvinceDistrictMaster'
)
BEGIN

                    UPDATE ds_divisions
                    SET 
                        DistrictId = (SELECT Id FROM Districts WHERE Name = ds_divisions.District),
                        ProvinceId = (SELECT Id FROM Provinces WHERE Name = ds_divisions.Province)
                    WHERE District IS NOT NULL AND Province IS NOT NULL;
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614170312_AddProvinceDistrictMaster'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ds_divisions]') AND [c].[name] = N'District');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [ds_divisions] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [ds_divisions] DROP COLUMN [District];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614170312_AddProvinceDistrictMaster'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ds_divisions]') AND [c].[name] = N'Province');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [ds_divisions] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [ds_divisions] DROP COLUMN [Province];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614170312_AddProvinceDistrictMaster'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260614170312_AddProvinceDistrictMaster', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616113527_AddGnAppointments'
)
BEGIN
    ALTER TABLE [appointments] DROP CONSTRAINT [PK_appointments];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616113527_AddGnAppointments'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[appointments]') AND [c].[name] = N'Id');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [appointments] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [appointments] DROP COLUMN [Id];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616113527_AddGnAppointments'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[appointments]') AND [c].[name] = N'Service');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [appointments] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [appointments] DROP COLUMN [Service];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616113527_AddGnAppointments'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[appointments]') AND [c].[name] = N'TimeSlot');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [appointments] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [appointments] DROP COLUMN [TimeSlot];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616113527_AddGnAppointments'
)
BEGIN
    ALTER TABLE [appointments] ADD [AppointmentId] int NOT NULL IDENTITY;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616113527_AddGnAppointments'
)
BEGIN
    ALTER TABLE [appointments] ADD [CancellationReason] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616113527_AddGnAppointments'
)
BEGIN
    ALTER TABLE [appointments] ADD [EndTime] time NOT NULL DEFAULT '00:00:00';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616113527_AddGnAppointments'
)
BEGIN
    ALTER TABLE [appointments] ADD [RescheduledFrom] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616113527_AddGnAppointments'
)
BEGIN
    ALTER TABLE [appointments] ADD [ServiceType] nvarchar(200) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616113527_AddGnAppointments'
)
BEGIN
    ALTER TABLE [appointments] ADD [StartTime] time NOT NULL DEFAULT '00:00:00';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616113527_AddGnAppointments'
)
BEGIN
    ALTER TABLE [appointments] ADD [UpdatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616113527_AddGnAppointments'
)
BEGIN
    ALTER TABLE [appointments] ADD CONSTRAINT [PK_appointments] PRIMARY KEY ([AppointmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616113527_AddGnAppointments'
)
BEGIN
    CREATE TABLE [gn_availability] (
        [AvailabilityId] int NOT NULL IDENTITY,
        [GnOfficerNic] nvarchar(20) NOT NULL,
        [AvailableDate] date NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [StartTime] time NULL,
        [EndTime] time NULL,
        [Notes] nvarchar(255) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_gn_availability] PRIMARY KEY ([AvailabilityId]),
        CONSTRAINT [FK_gn_availability_gn_officers_GnOfficerNic] FOREIGN KEY ([GnOfficerNic]) REFERENCES [gn_officers] ([Nic]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616113527_AddGnAppointments'
)
BEGIN
    CREATE UNIQUE INDEX [IX_gn_availability_GnOfficerNic_AvailableDate] ON [gn_availability] ([GnOfficerNic], [AvailableDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616113527_AddGnAppointments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260616113527_AddGnAppointments', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617174939_AddGNDocuments'
)
BEGIN
    DROP TABLE [download_forms];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617174939_AddGNDocuments'
)
BEGIN
    DROP TABLE [gn_leaves_holidays];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617174939_AddGNDocuments'
)
BEGIN
    ALTER TABLE [document_categories] DROP CONSTRAINT [PK_document_categories];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617174939_AddGNDocuments'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[document_categories]') AND [c].[name] = N'Id');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [document_categories] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [document_categories] DROP COLUMN [Id];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617174939_AddGNDocuments'
)
BEGIN
    EXEC sp_rename N'[document_categories].[Name]', N'CategoryName', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617174939_AddGNDocuments'
)
BEGIN
    EXEC sp_rename N'[document_categories].[IX_document_categories_Name]', N'IX_document_categories_CategoryName', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617174939_AddGNDocuments'
)
BEGIN
    ALTER TABLE [document_categories] ADD [CategoryId] int NOT NULL IDENTITY;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617174939_AddGNDocuments'
)
BEGIN
    ALTER TABLE [document_categories] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617174939_AddGNDocuments'
)
BEGIN
    ALTER TABLE [document_categories] ADD CONSTRAINT [PK_document_categories] PRIMARY KEY ([CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617174939_AddGNDocuments'
)
BEGIN
    CREATE TABLE [gn_documents] (
        [DocumentId] int NOT NULL IDENTITY,
        [GnOfficerNic] nvarchar(20) NOT NULL,
        [DivisionId] nvarchar(50) NOT NULL,
        [Title] nvarchar(255) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [CategoryId] int NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [FilePath] nvarchar(500) NOT NULL,
        [FileType] nvarchar(50) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [PublishDate] datetime2 NULL,
        [ExpiryDate] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_gn_documents] PRIMARY KEY ([DocumentId]),
        CONSTRAINT [FK_gn_documents_document_categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [document_categories] ([CategoryId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_gn_documents_gn_divisions_DivisionId] FOREIGN KEY ([DivisionId]) REFERENCES [gn_divisions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_gn_documents_gn_officers_GnOfficerNic] FOREIGN KEY ([GnOfficerNic]) REFERENCES [gn_officers] ([Nic]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617174939_AddGNDocuments'
)
BEGIN
    CREATE TABLE [gn_document_logs] (
        [LogId] int NOT NULL IDENTITY,
        [DocumentId] int NOT NULL,
        [Action] nvarchar(50) NOT NULL,
        [ActionDate] datetime2 NOT NULL,
        [PerformedByNic] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_gn_document_logs] PRIMARY KEY ([LogId]),
        CONSTRAINT [FK_gn_document_logs_gn_documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [gn_documents] ([DocumentId]) ON DELETE CASCADE,
        CONSTRAINT [FK_gn_document_logs_gn_officers_PerformedByNic] FOREIGN KEY ([PerformedByNic]) REFERENCES [gn_officers] ([Nic]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617174939_AddGNDocuments'
)
BEGIN
    CREATE INDEX [IX_gn_document_logs_DocumentId] ON [gn_document_logs] ([DocumentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617174939_AddGNDocuments'
)
BEGIN
    CREATE INDEX [IX_gn_document_logs_PerformedByNic] ON [gn_document_logs] ([PerformedByNic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617174939_AddGNDocuments'
)
BEGIN
    CREATE INDEX [IX_gn_documents_CategoryId] ON [gn_documents] ([CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617174939_AddGNDocuments'
)
BEGIN
    CREATE INDEX [IX_gn_documents_DivisionId] ON [gn_documents] ([DivisionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617174939_AddGNDocuments'
)
BEGIN
    CREATE INDEX [IX_gn_documents_GnOfficerNic] ON [gn_documents] ([GnOfficerNic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617174939_AddGNDocuments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260617174939_AddGNDocuments', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618065047_AddDocumentVaultDocuments'
)
BEGIN
    CREATE TABLE [document_vault_documents] (
        [VaultDocumentId] int NOT NULL IDENTITY,
        [NIC] nvarchar(20) NOT NULL,
        [roleId] nvarchar(50) NOT NULL,
        [CategoryId] int NOT NULL,
        [DocumentName] nvarchar(255) NOT NULL,
        [OriginalFileName] nvarchar(255) NOT NULL,
        [FilePath] nvarchar(500) NOT NULL,
        [FileType] nvarchar(50) NOT NULL,
        [FileSize] bigint NULL,
        [Description] nvarchar(500) NOT NULL,
        [IsActive] bit NOT NULL,
        [UploadedAt] datetime2 NOT NULL,
        [UploadedBy] int NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] int NULL,
        CONSTRAINT [PK_document_vault_documents] PRIMARY KEY ([VaultDocumentId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618065047_AddDocumentVaultDocuments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260618065047_AddDocumentVaultDocuments', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    ALTER TABLE [conversations] DROP CONSTRAINT [FK_conversations_service_requests_RequestId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    ALTER TABLE [gn_document_logs] DROP CONSTRAINT [FK_gn_document_logs_gn_documents_DocumentId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    ALTER TABLE [Requests] DROP CONSTRAINT [FK_Requests_gn_officers_AssignedGnDivisionNic];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    DROP TABLE [request_attachments];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    DROP TABLE [request_remarks];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    DROP TABLE [request_timelines];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    DROP TABLE [service_requests];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    DROP INDEX [IX_gn_document_logs_DocumentId] ON [gn_document_logs];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    DROP INDEX [IX_conversations_RequestId] ON [conversations];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[document_vault_documents]') AND [c].[name] = N'CategoryId');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [document_vault_documents] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [document_vault_documents] DROP COLUMN [CategoryId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    DECLARE @var8 sysname;
    SELECT @var8 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[document_vault_documents]') AND [c].[name] = N'DeletedAt');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [document_vault_documents] DROP CONSTRAINT [' + @var8 + '];');
    ALTER TABLE [document_vault_documents] DROP COLUMN [DeletedAt];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    DECLARE @var9 sysname;
    SELECT @var9 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[document_vault_documents]') AND [c].[name] = N'DeletedBy');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [document_vault_documents] DROP CONSTRAINT [' + @var9 + '];');
    ALTER TABLE [document_vault_documents] DROP COLUMN [DeletedBy];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    DECLARE @var10 sysname;
    SELECT @var10 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[document_vault_documents]') AND [c].[name] = N'FileSize');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [document_vault_documents] DROP CONSTRAINT [' + @var10 + '];');
    ALTER TABLE [document_vault_documents] DROP COLUMN [FileSize];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    EXEC sp_rename N'[Requests].[AssignedGnDivisionNic]', N'AssignedOfficerNic', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    EXEC sp_rename N'[Requests].[IX_Requests_AssignedGnDivisionNic]', N'IX_Requests_AssignedOfficerNic', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    ALTER TABLE [document_vault_documents] ADD [CategoryName] nvarchar(100) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    CREATE TABLE [document_vault_categories] (
        [Id] int NOT NULL IDENTITY,
        [NIC] nvarchar(20) NOT NULL,
        [RoleId] nvarchar(50) NOT NULL,
        [CategoriesJson] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_document_vault_categories] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    CREATE TABLE [gn_announcements] (
        [AnnouncementId] int NOT NULL IDENTITY,
        [GnOfficerNic] nvarchar(20) NOT NULL,
        [GnDivisionId] nvarchar(50) NOT NULL,
        [AnnouncementType] nvarchar(50) NOT NULL,
        [Title] nvarchar(255) NOT NULL,
        [Description] nvarchar(max) NULL,
        [DocumentId] int NULL,
        [ExternalSurveyUrl] nvarchar(max) NULL,
        [PollQuestion] nvarchar(max) NULL,
        [PublishDate] datetime2 NULL,
        [ExpiryDate] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(20) NULL,
        [ModifiedBy] nvarchar(20) NULL,
        CONSTRAINT [PK_gn_announcements] PRIMARY KEY ([AnnouncementId]),
        CONSTRAINT [FK_gn_announcements_gn_divisions_GnDivisionId] FOREIGN KEY ([GnDivisionId]) REFERENCES [gn_divisions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_gn_announcements_gn_documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [gn_documents] ([DocumentId]),
        CONSTRAINT [FK_gn_announcements_gn_officers_GnOfficerNic] FOREIGN KEY ([GnOfficerNic]) REFERENCES [gn_officers] ([Nic]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    CREATE TABLE [gn_poll_options] (
        [OptionId] int NOT NULL IDENTITY,
        [AnnouncementId] int NOT NULL,
        [OptionText] nvarchar(255) NOT NULL,
        CONSTRAINT [PK_gn_poll_options] PRIMARY KEY ([OptionId]),
        CONSTRAINT [FK_gn_poll_options_gn_announcements_AnnouncementId] FOREIGN KEY ([AnnouncementId]) REFERENCES [gn_announcements] ([AnnouncementId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    CREATE TABLE [gn_poll_votes] (
        [VoteId] int NOT NULL IDENTITY,
        [AnnouncementId] int NOT NULL,
        [OptionId] int NOT NULL,
        [CitizenNic] nvarchar(20) NOT NULL,
        [VotedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_gn_poll_votes] PRIMARY KEY ([VoteId]),
        CONSTRAINT [FK_gn_poll_votes_Citizen_Master_CitizenNic] FOREIGN KEY ([CitizenNic]) REFERENCES [Citizen_Master] ([Nic]) ON DELETE CASCADE,
        CONSTRAINT [FK_gn_poll_votes_gn_announcements_AnnouncementId] FOREIGN KEY ([AnnouncementId]) REFERENCES [gn_announcements] ([AnnouncementId]) ON DELETE CASCADE,
        CONSTRAINT [FK_gn_poll_votes_gn_poll_options_OptionId] FOREIGN KEY ([OptionId]) REFERENCES [gn_poll_options] ([OptionId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    CREATE INDEX [IX_gn_announcements_DocumentId] ON [gn_announcements] ([DocumentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    CREATE INDEX [IX_gn_announcements_GnDivisionId] ON [gn_announcements] ([GnDivisionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    CREATE INDEX [IX_gn_announcements_GnOfficerNic] ON [gn_announcements] ([GnOfficerNic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    CREATE INDEX [IX_gn_poll_options_AnnouncementId] ON [gn_poll_options] ([AnnouncementId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    CREATE UNIQUE INDEX [IX_gn_poll_votes_AnnouncementId_CitizenNic] ON [gn_poll_votes] ([AnnouncementId], [CitizenNic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    CREATE INDEX [IX_gn_poll_votes_CitizenNic] ON [gn_poll_votes] ([CitizenNic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    CREATE INDEX [IX_gn_poll_votes_OptionId] ON [gn_poll_votes] ([OptionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    ALTER TABLE [Requests] ADD CONSTRAINT [FK_Requests_gn_officers_AssignedOfficerNic] FOREIGN KEY ([AssignedOfficerNic]) REFERENCES [gn_officers] ([Nic]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629121810_UpdateCitizenServiceRequestSchema'
)
BEGIN
    ALTER TABLE [Requests] DROP CONSTRAINT [FK_Requests_gn_officers_AssignedOfficerNic];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629121810_UpdateCitizenServiceRequestSchema'
)
BEGIN
    ALTER TABLE [Request_Data] DROP CONSTRAINT [PK_Request_Data];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629121810_UpdateCitizenServiceRequestSchema'
)
BEGIN
    DROP INDEX [IX_Request_Data_RequestId] ON [Request_Data];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629121810_UpdateCitizenServiceRequestSchema'
)
BEGIN
    DECLARE @var11 sysname;
    SELECT @var11 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Request_Data]') AND [c].[name] = N'Id');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Request_Data] DROP CONSTRAINT [' + @var11 + '];');
    ALTER TABLE [Request_Data] DROP COLUMN [Id];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629121810_UpdateCitizenServiceRequestSchema'
)
BEGIN
    EXEC sp_rename N'[Requests].[AssignedOfficerNic]', N'AssignedGnOfficerNic', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629121810_UpdateCitizenServiceRequestSchema'
)
BEGIN
    EXEC sp_rename N'[Requests].[IX_Requests_AssignedOfficerNic]', N'IX_Requests_AssignedGnOfficerNic', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629121810_UpdateCitizenServiceRequestSchema'
)
BEGIN
    ALTER TABLE [Requests] ADD [FinalDocumentUrl] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629121810_UpdateCitizenServiceRequestSchema'
)
BEGIN
    ALTER TABLE [Request_Data] ADD CONSTRAINT [PK_Request_Data] PRIMARY KEY ([RequestId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629121810_UpdateCitizenServiceRequestSchema'
)
BEGIN
    ALTER TABLE [Requests] ADD CONSTRAINT [FK_Requests_gn_officers_AssignedGnOfficerNic] FOREIGN KEY ([AssignedGnOfficerNic]) REFERENCES [gn_officers] ([Nic]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629121810_UpdateCitizenServiceRequestSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260629121810_UpdateCitizenServiceRequestSchema', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629122705_UpdateRequestStatusHistoryAndAttachments'
)
BEGIN
    ALTER TABLE [Requests] DROP CONSTRAINT [FK_Requests_gn_officers_AssignedGnOfficerNic];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629122705_UpdateRequestStatusHistoryAndAttachments'
)
BEGIN
    ALTER TABLE [Request_Status_History] DROP CONSTRAINT [PK_Request_Status_History];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629122705_UpdateRequestStatusHistoryAndAttachments'
)
BEGIN
    DROP INDEX [IX_Request_Status_History_RequestId] ON [Request_Status_History];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629122705_UpdateRequestStatusHistoryAndAttachments'
)
BEGIN
    DECLARE @var12 sysname;
    SELECT @var12 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Request_Status_History]') AND [c].[name] = N'Id');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Request_Status_History] DROP CONSTRAINT [' + @var12 + '];');
    ALTER TABLE [Request_Status_History] DROP COLUMN [Id];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629122705_UpdateRequestStatusHistoryAndAttachments'
)
BEGIN
    ALTER TABLE [Request_Status_History] ADD CONSTRAINT [PK_Request_Status_History] PRIMARY KEY ([RequestId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629122705_UpdateRequestStatusHistoryAndAttachments'
)
BEGIN
    ALTER TABLE [Requests] ADD CONSTRAINT [FK_Requests_gn_officers_AssignedGnOfficerNic] FOREIGN KEY ([AssignedGnOfficerNic]) REFERENCES [gn_officers] ([Nic]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629122705_UpdateRequestStatusHistoryAndAttachments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260629122705_UpdateRequestStatusHistoryAndAttachments', N'8.0.10');
END;
GO

COMMIT;
GO

