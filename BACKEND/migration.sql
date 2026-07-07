BEGIN TRANSACTION;
GO

ALTER TABLE [conversations] DROP CONSTRAINT [FK_conversations_service_requests_RequestId];
GO

ALTER TABLE [gn_document_logs] DROP CONSTRAINT [FK_gn_document_logs_gn_documents_DocumentId];
GO

ALTER TABLE [Requests] DROP CONSTRAINT [FK_Requests_gn_officers_AssignedGnDivisionNic];
GO

DROP TABLE [request_attachments];
GO

DROP TABLE [request_remarks];
GO

DROP TABLE [request_timelines];
GO

DROP TABLE [service_requests];
GO

DROP INDEX [IX_gn_document_logs_DocumentId] ON [gn_document_logs];
GO

DROP INDEX [IX_conversations_RequestId] ON [conversations];
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[document_vault_documents]') AND [c].[name] = N'CategoryId');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [document_vault_documents] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [document_vault_documents] DROP COLUMN [CategoryId];
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[document_vault_documents]') AND [c].[name] = N'DeletedAt');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [document_vault_documents] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [document_vault_documents] DROP COLUMN [DeletedAt];
GO

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[document_vault_documents]') AND [c].[name] = N'DeletedBy');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [document_vault_documents] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [document_vault_documents] DROP COLUMN [DeletedBy];
GO

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[document_vault_documents]') AND [c].[name] = N'FileSize');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [document_vault_documents] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [document_vault_documents] DROP COLUMN [FileSize];
GO

EXEC sp_rename N'[Requests].[AssignedGnDivisionNic]', N'AssignedOfficerNic', N'COLUMN';
GO

EXEC sp_rename N'[Requests].[IX_Requests_AssignedGnDivisionNic]', N'IX_Requests_AssignedOfficerNic', N'INDEX';
GO

ALTER TABLE [document_vault_documents] ADD [CategoryName] nvarchar(100) NOT NULL DEFAULT N'';
GO

CREATE TABLE [document_vault_categories] (
    [Id] int NOT NULL IDENTITY,
    [NIC] nvarchar(20) NOT NULL,
    [RoleId] nvarchar(50) NOT NULL,
    [CategoriesJson] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_document_vault_categories] PRIMARY KEY ([Id])
);
GO

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
GO

CREATE TABLE [gn_poll_options] (
    [OptionId] int NOT NULL IDENTITY,
    [AnnouncementId] int NOT NULL,
    [OptionText] nvarchar(255) NOT NULL,
    CONSTRAINT [PK_gn_poll_options] PRIMARY KEY ([OptionId]),
    CONSTRAINT [FK_gn_poll_options_gn_announcements_AnnouncementId] FOREIGN KEY ([AnnouncementId]) REFERENCES [gn_announcements] ([AnnouncementId]) ON DELETE CASCADE
);
GO

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
GO

CREATE INDEX [IX_gn_announcements_DocumentId] ON [gn_announcements] ([DocumentId]);
GO

CREATE INDEX [IX_gn_announcements_GnDivisionId] ON [gn_announcements] ([GnDivisionId]);
GO

CREATE INDEX [IX_gn_announcements_GnOfficerNic] ON [gn_announcements] ([GnOfficerNic]);
GO

CREATE INDEX [IX_gn_poll_options_AnnouncementId] ON [gn_poll_options] ([AnnouncementId]);
GO

CREATE UNIQUE INDEX [IX_gn_poll_votes_AnnouncementId_CitizenNic] ON [gn_poll_votes] ([AnnouncementId], [CitizenNic]);
GO

CREATE INDEX [IX_gn_poll_votes_CitizenNic] ON [gn_poll_votes] ([CitizenNic]);
GO

CREATE INDEX [IX_gn_poll_votes_OptionId] ON [gn_poll_votes] ([OptionId]);
GO

ALTER TABLE [Requests] ADD CONSTRAINT [FK_Requests_gn_officers_AssignedOfficerNic] FOREIGN KEY ([AssignedOfficerNic]) REFERENCES [gn_officers] ([Nic]) ON DELETE SET NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260629115918_RemoveServiceRequestAndRenameAssignedOfficer', N'8.0.10');
GO

COMMIT;
GO

