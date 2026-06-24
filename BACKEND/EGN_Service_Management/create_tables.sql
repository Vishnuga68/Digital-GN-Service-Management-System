CREATE TABLE [document_categories] (
    [CategoryId] int NOT NULL IDENTITY,
    [CategoryName] nvarchar(100) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetime2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT [PK_document_categories] PRIMARY KEY ([CategoryId])
);

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

CREATE INDEX [IX_gn_document_logs_DocumentId] ON [gn_document_logs] ([DocumentId]);
CREATE INDEX [IX_gn_document_logs_PerformedByNic] ON [gn_document_logs] ([PerformedByNic]);
CREATE INDEX [IX_gn_documents_CategoryId] ON [gn_documents] ([CategoryId]);
CREATE INDEX [IX_gn_documents_DivisionId] ON [gn_documents] ([DivisionId]);
CREATE INDEX [IX_gn_documents_GnOfficerNic] ON [gn_documents] ([GnOfficerNic]);
