BEGIN TRANSACTION;
GO

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
GO

CREATE INDEX [IX_gn_division_assignments_GnDivisionId] ON [gn_division_assignments] ([GnDivisionId]);
GO

CREATE INDEX [IX_gn_division_assignments_GnOfficerNic] ON [gn_division_assignments] ([GnOfficerNic]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260529050505_AddGnDivisionAssignment', N'8.0.10');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Citizen_Master] ADD [GnDivisionId] nvarchar(50) NULL;
GO

CREATE INDEX [IX_Citizen_Master_GnDivisionId] ON [Citizen_Master] ([GnDivisionId]);
GO

ALTER TABLE [Citizen_Master] ADD CONSTRAINT [FK_Citizen_Master_gn_divisions_GnDivisionId] FOREIGN KEY ([GnDivisionId]) REFERENCES [gn_divisions] ([Id]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260529062948_AddGnDivisionToCitizenMaster', N'8.0.10');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [appointments] DROP CONSTRAINT [FK_appointments_EGN_Service_User_CitizenNic];
GO

ALTER TABLE [conversations] DROP CONSTRAINT [FK_conversations_EGN_Service_User_CitizenNic];
GO

ALTER TABLE [poll_votes] DROP CONSTRAINT [FK_poll_votes_EGN_Service_User_CitizenNic];
GO

ALTER TABLE [survey_responses] DROP CONSTRAINT [FK_survey_responses_EGN_Service_User_CitizenNic];
GO

DROP TABLE [EGN_Service_User];
GO

CREATE TABLE [EGN_Service_Users] (
    [Nic] nvarchar(20) NOT NULL,
    [RegisteredAt] datetime2 NOT NULL,
    CONSTRAINT [PK_EGN_Service_Users] PRIMARY KEY ([Nic]),
    CONSTRAINT [FK_EGN_Service_Users_Citizen_Master_Nic] FOREIGN KEY ([Nic]) REFERENCES [Citizen_Master] ([Nic]) ON DELETE CASCADE,
    CONSTRAINT [FK_EGN_Service_Users_accounts_Nic] FOREIGN KEY ([Nic]) REFERENCES [accounts] ([Nic]) ON DELETE CASCADE
);
GO

ALTER TABLE [appointments] ADD CONSTRAINT [FK_appointments_EGN_Service_Users_CitizenNic] FOREIGN KEY ([CitizenNic]) REFERENCES [EGN_Service_Users] ([Nic]) ON DELETE CASCADE;
GO

ALTER TABLE [conversations] ADD CONSTRAINT [FK_conversations_EGN_Service_Users_CitizenNic] FOREIGN KEY ([CitizenNic]) REFERENCES [EGN_Service_Users] ([Nic]) ON DELETE CASCADE;
GO

ALTER TABLE [poll_votes] ADD CONSTRAINT [FK_poll_votes_EGN_Service_Users_CitizenNic] FOREIGN KEY ([CitizenNic]) REFERENCES [EGN_Service_Users] ([Nic]) ON DELETE CASCADE;
GO

ALTER TABLE [survey_responses] ADD CONSTRAINT [FK_survey_responses_EGN_Service_Users_CitizenNic] FOREIGN KEY ([CitizenNic]) REFERENCES [EGN_Service_Users] ([Nic]) ON DELETE CASCADE;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260605154118_RemoveRoleFromAccounts', N'8.0.10');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Citizen_Master]') AND [c].[name] = N'RelationshipToHead');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Citizen_Master] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [Citizen_Master] DROP COLUMN [RelationshipToHead];
GO

EXEC sp_rename N'[citizen_special_statuses].[HasOtherDisability]', N'HasVoterId', N'COLUMN';
GO

ALTER TABLE [citizen_special_statuses] ADD [HasElderlyEligible] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260605181715_RenameHasOtherDisabilityToHasElderlyEligible', N'8.0.10');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Provinces] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_Provinces] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Districts] (
    [Id] int NOT NULL IDENTITY,
    [ProvinceId] int NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_Districts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Districts_Provinces_ProvinceId] FOREIGN KEY ([ProvinceId]) REFERENCES [Provinces] ([Id]) ON DELETE NO ACTION
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Provinces]'))
    SET IDENTITY_INSERT [Provinces] ON;
INSERT INTO [Provinces] ([Id], [Name])
VALUES (1, N'Northern'),
(2, N'Eastern'),
(3, N'North Western'),
(4, N'North Central'),
(5, N'Central'),
(6, N'Western'),
(7, N'Southern'),
(8, N'Uva'),
(9, N'Sabaragamuwa');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Provinces]'))
    SET IDENTITY_INSERT [Provinces] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name', N'ProvinceId') AND [object_id] = OBJECT_ID(N'[Districts]'))
    SET IDENTITY_INSERT [Districts] ON;
INSERT INTO [Districts] ([Id], [Name], [ProvinceId])
VALUES (1, N'Jaffna', 1),
(2, N'Kilinochchi', 1),
(3, N'Mannar', 1),
(4, N'Mullaitivu', 1),
(5, N'Vavuniya', 1),
(6, N'Trincomalee', 2),
(7, N'Batticaloa', 2),
(8, N'Ampara', 2),
(9, N'Kurunegala', 3),
(10, N'Puttalam', 3),
(11, N'Anuradhapura', 4),
(12, N'Polonnaruwa', 4),
(13, N'Kandy', 5),
(14, N'Matale', 5),
(15, N'Nuwara Eliya', 5),
(16, N'Colombo', 6),
(17, N'Gampaha', 6),
(18, N'Kalutara', 6),
(19, N'Galle', 7),
(20, N'Matara', 7),
(21, N'Hambantota', 7),
(22, N'Badulla', 8),
(23, N'Monaragala', 8),
(24, N'Ratnapura', 9),
(25, N'Kegalle', 9);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name', N'ProvinceId') AND [object_id] = OBJECT_ID(N'[Districts]'))
    SET IDENTITY_INSERT [Districts] OFF;
GO

ALTER TABLE [ds_divisions] ADD [DistrictId] int NULL;
GO

ALTER TABLE [ds_divisions] ADD [ProvinceId] int NULL;
GO

CREATE INDEX [IX_Districts_ProvinceId] ON [Districts] ([ProvinceId]);
GO

CREATE INDEX [IX_ds_divisions_DistrictId] ON [ds_divisions] ([DistrictId]);
GO

CREATE INDEX [IX_ds_divisions_ProvinceId] ON [ds_divisions] ([ProvinceId]);
GO

ALTER TABLE [ds_divisions] ADD CONSTRAINT [FK_ds_divisions_Districts_DistrictId] FOREIGN KEY ([DistrictId]) REFERENCES [Districts] ([Id]) ON DELETE NO ACTION;
GO

ALTER TABLE [ds_divisions] ADD CONSTRAINT [FK_ds_divisions_Provinces_ProvinceId] FOREIGN KEY ([ProvinceId]) REFERENCES [Provinces] ([Id]) ON DELETE NO ACTION;
GO


                UPDATE ds_divisions
                SET 
                    DistrictId = (SELECT Id FROM Districts WHERE Name = ds_divisions.District),
                    ProvinceId = (SELECT Id FROM Provinces WHERE Name = ds_divisions.Province)
                WHERE District IS NOT NULL AND Province IS NOT NULL;
            
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ds_divisions]') AND [c].[name] = N'District');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [ds_divisions] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [ds_divisions] DROP COLUMN [District];
GO

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ds_divisions]') AND [c].[name] = N'Province');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [ds_divisions] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [ds_divisions] DROP COLUMN [Province];
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260614170312_AddProvinceDistrictMaster', N'8.0.10');
GO

COMMIT;
GO

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

