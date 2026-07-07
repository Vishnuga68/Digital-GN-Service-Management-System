BEGIN TRANSACTION;
GO

ALTER TABLE [Requests] DROP CONSTRAINT [FK_Requests_gn_officers_AssignedGnOfficerNic];
GO

ALTER TABLE [Request_Status_History] DROP CONSTRAINT [PK_Request_Status_History];
GO

DROP INDEX [IX_Request_Status_History_RequestId] ON [Request_Status_History];
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Request_Status_History]') AND [c].[name] = N'Id');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Request_Status_History] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [Request_Status_History] DROP COLUMN [Id];
GO

ALTER TABLE [Request_Status_History] ADD CONSTRAINT [PK_Request_Status_History] PRIMARY KEY ([RequestId]);
GO

ALTER TABLE [Requests] ADD CONSTRAINT [FK_Requests_gn_officers_AssignedGnOfficerNic] FOREIGN KEY ([AssignedGnOfficerNic]) REFERENCES [gn_officers] ([Nic]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260629122705_UpdateRequestStatusHistoryAndAttachments', N'8.0.10');
GO

COMMIT;
GO

