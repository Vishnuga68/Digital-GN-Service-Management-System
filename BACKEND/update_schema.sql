BEGIN TRANSACTION;
GO

ALTER TABLE [Requests] DROP CONSTRAINT [FK_Requests_gn_officers_AssignedOfficerNic];
GO

ALTER TABLE [Request_Data] DROP CONSTRAINT [PK_Request_Data];
GO

DROP INDEX [IX_Request_Data_RequestId] ON [Request_Data];
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Request_Data]') AND [c].[name] = N'Id');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Request_Data] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [Request_Data] DROP COLUMN [Id];
GO

EXEC sp_rename N'[Requests].[AssignedOfficerNic]', N'AssignedGnOfficerNic', N'COLUMN';
GO

EXEC sp_rename N'[Requests].[IX_Requests_AssignedOfficerNic]', N'IX_Requests_AssignedGnOfficerNic', N'INDEX';
GO

ALTER TABLE [Requests] ADD [FinalDocumentUrl] nvarchar(max) NULL;
GO

ALTER TABLE [Request_Data] ADD CONSTRAINT [PK_Request_Data] PRIMARY KEY ([RequestId]);
GO

ALTER TABLE [Requests] ADD CONSTRAINT [FK_Requests_gn_officers_AssignedGnOfficerNic] FOREIGN KEY ([AssignedGnOfficerNic]) REFERENCES [gn_officers] ([Nic]) ON DELETE SET NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260629121810_UpdateCitizenServiceRequestSchema', N'8.0.10');
GO

COMMIT;
GO

