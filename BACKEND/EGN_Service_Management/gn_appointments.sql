BEGIN TRANSACTION;
GO

ALTER TABLE [appointments] DROP CONSTRAINT [PK_appointments];
GO

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[appointments]') AND [c].[name] = N'Id');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [appointments] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [appointments] DROP COLUMN [Id];
GO

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[appointments]') AND [c].[name] = N'Service');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [appointments] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [appointments] DROP COLUMN [Service];
GO

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[appointments]') AND [c].[name] = N'TimeSlot');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [appointments] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [appointments] DROP COLUMN [TimeSlot];
GO

ALTER TABLE [appointments] ADD [AppointmentId] int NOT NULL IDENTITY;
GO

ALTER TABLE [appointments] ADD [CancellationReason] nvarchar(max) NULL;
GO

ALTER TABLE [appointments] ADD [EndTime] time NOT NULL DEFAULT '00:00:00';
GO

ALTER TABLE [appointments] ADD [RescheduledFrom] int NULL;
GO

ALTER TABLE [appointments] ADD [ServiceType] nvarchar(200) NOT NULL DEFAULT N'';
GO

ALTER TABLE [appointments] ADD [StartTime] time NOT NULL DEFAULT '00:00:00';
GO

ALTER TABLE [appointments] ADD [UpdatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

ALTER TABLE [appointments] ADD CONSTRAINT [PK_appointments] PRIMARY KEY ([AppointmentId]);
GO

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
GO

CREATE UNIQUE INDEX [IX_gn_availability_GnOfficerNic_AvailableDate] ON [gn_availability] ([GnOfficerNic], [AvailableDate]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260616113527_AddGnAppointments', N'8.0.10');
GO

COMMIT;
GO
