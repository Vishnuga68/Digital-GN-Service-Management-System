CREATE TABLE [gn_officer_digital_details] (
    [GNOfficerNIC] nvarchar(20) NOT NULL,
    [RubberStampPath] nvarchar(500) NULL,
    [SignaturePath] nvarchar(500) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NULL,
    [CreatedBy] nvarchar(100) NULL,
    [UpdatedBy] nvarchar(100) NULL,
    CONSTRAINT [PK_gn_officer_digital_details] PRIMARY KEY ([GNOfficerNIC]),
    CONSTRAINT [FK_gn_officer_digital_details_gn_officers_GNOfficerNIC] FOREIGN KEY ([GNOfficerNIC]) REFERENCES [gn_officers] ([Nic]) ON DELETE CASCADE
);
