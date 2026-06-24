BEGIN TRANSACTION;
GO

DROP TABLE [download_forms];
GO

DROP TABLE [gn_leaves_holidays];
GO

ALTER TABLE [document_categories] DROP CONSTRAINT [PK_document_categories];
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[document_categories]') AND [c].[name] = N'Id');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [document_categories] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [document_categories] DROP COLUMN [Id];
GO

EXEC sp_rename N'[document_categories].[Name]', N'CategoryName', N'COLUMN';
GO

EXEC sp_rename N'[document_categories].[IX_document_categories_Name]', N'IX_document_categories_CategoryName', N'INDEX';
GO

ALTER TABLE [document_categories] ADD [CategoryId] int NOT NULL IDENTITY;
GO

ALTER TABLE [document_categories] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [document_categories] ADD CONSTRAINT [PK_document_categories] PRIMARY KEY ([CategoryId]);
GO

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
GO

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
GO

CREATE INDEX [IX_gn_document_logs_DocumentId] ON [gn_document_logs] ([DocumentId]);
GO

CREATE INDEX [IX_gn_document_logs_PerformedByNic] ON [gn_document_logs] ([PerformedByNic]);
GO

CREATE INDEX [IX_gn_documents_CategoryId] ON [gn_documents] ([CategoryId]);
GO

CREATE INDEX [IX_gn_documents_DivisionId] ON [gn_documents] ([DivisionId]);
GO

CREATE INDEX [IX_gn_documents_GnOfficerNic] ON [gn_documents] ([GnOfficerNic]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260617174939_AddGNDocuments', N'8.0.10');
GO

COMMIT;
GO

