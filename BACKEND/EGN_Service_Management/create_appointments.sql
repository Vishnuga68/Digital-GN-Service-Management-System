BEGIN TRANSACTION;
GO

CREATE TABLE [appointments] (
    [AppointmentId] int NOT NULL IDENTITY,
    [CitizenNic] nvarchar(20) NOT NULL,
    [GnOfficerNic] nvarchar(20) NOT NULL,
    [AppointmentDate] date NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [ServiceType] nvarchar(200) NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [Notes] nvarchar(255) NULL,
    [CancellationReason] nvarchar(max) NULL,
    [RescheduledFrom] int NULL,
    CONSTRAINT [PK_appointments] PRIMARY KEY ([AppointmentId])
);
GO

COMMIT;
GO
