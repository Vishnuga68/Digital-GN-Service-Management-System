-- Create Chat_Conversations Table
CREATE TABLE [Chat_Conversations] (
    [ConversationId] int IDENTITY(1,1) NOT NULL,
    [ChatType] nvarchar(50) NOT NULL,
    [CitizenNic] nvarchar(20) NOT NULL,
    [GnOfficerNic] nvarchar(20) NOT NULL,
    [RequestId] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [LastMessageDate] datetime2 NULL,
    [CreatedByNic] nvarchar(20) NOT NULL,
    [IsActive] bit NOT NULL CONSTRAINT [DF_Chat_Conversations_IsActive] DEFAULT 1,
    [IsClosed] bit NOT NULL CONSTRAINT [DF_Chat_Conversations_IsClosed] DEFAULT 0,
    [ClosedDate] datetime2 NULL,
    CONSTRAINT [PK_Chat_Conversations] PRIMARY KEY ([ConversationId]),
    CONSTRAINT [FK_Chat_Conversations_Citizen_Master_CitizenNic] FOREIGN KEY ([CitizenNic]) REFERENCES [Citizen_Master] ([Nic]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Chat_Conversations_gn_officers_GnOfficerNic] FOREIGN KEY ([GnOfficerNic]) REFERENCES [gn_officers] ([Nic]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Chat_Conversations_Requests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [Requests] ([RequestId]) ON DELETE NO ACTION
);

-- Indexes for Chat_Conversations
CREATE INDEX [IX_Chat_Conversations_CitizenNic] ON [Chat_Conversations] ([CitizenNic]);
CREATE INDEX [IX_Chat_Conversations_GnOfficerNic] ON [Chat_Conversations] ([GnOfficerNic]);
CREATE INDEX [IX_Chat_Conversations_RequestId] ON [Chat_Conversations] ([RequestId]);
CREATE INDEX [IX_Chat_Conversations_LastMessageDate] ON [Chat_Conversations] ([LastMessageDate]);
CREATE INDEX [IX_Chat_Conversations_ChatType] ON [Chat_Conversations] ([ChatType]);

-- Uniqueness constraints (Filtered Indexes to handle nullable RequestId)
-- Common chat uniqueness: CitizenNic + GnOfficerNic + ChatType when RequestId is null
CREATE UNIQUE INDEX [UX_Chat_Conversations_Common] ON [Chat_Conversations] ([CitizenNic], [GnOfficerNic], [ChatType]) WHERE [RequestId] IS NULL;
-- Request support chat uniqueness: RequestId + ChatType when RequestId is not null
CREATE UNIQUE INDEX [UX_Chat_Conversations_RequestSupport] ON [Chat_Conversations] ([RequestId], [ChatType]) WHERE [RequestId] IS NOT NULL;


-- Create Chat_Messages Table
CREATE TABLE [Chat_Messages] (
    [MessageId] int IDENTITY(1,1) NOT NULL,
    [ConversationId] int NOT NULL,
    [SenderNic] nvarchar(20) NOT NULL,
    [SenderRole] nvarchar(50) NOT NULL,
    [MessageText] nvarchar(max) NULL,
    [MessageType] nvarchar(50) NOT NULL,
    [SentDate] datetime2 NOT NULL,
    [IsRead] bit NOT NULL CONSTRAINT [DF_Chat_Messages_IsRead] DEFAULT 0,
    [ReadDate] datetime2 NULL,
    [IsDeleted] bit NOT NULL CONSTRAINT [DF_Chat_Messages_IsDeleted] DEFAULT 0,
    [DeletedDate] datetime2 NULL,
    CONSTRAINT [PK_Chat_Messages] PRIMARY KEY ([MessageId]),
    CONSTRAINT [FK_Chat_Messages_Chat_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Chat_Conversations] ([ConversationId]) ON DELETE NO ACTION
);

-- Indexes for Chat_Messages
CREATE INDEX [IX_Chat_Messages_ConversationId] ON [Chat_Messages] ([ConversationId]);
CREATE INDEX [IX_Chat_Messages_SenderNic] ON [Chat_Messages] ([SenderNic]);
CREATE INDEX [IX_Chat_Messages_SentDate] ON [Chat_Messages] ([SentDate]);
CREATE INDEX [IX_Chat_Messages_IsRead] ON [Chat_Messages] ([IsRead]);


-- Create Chat_Attachments Table
CREATE TABLE [Chat_Attachments] (
    [AttachmentId] int IDENTITY(1,1) NOT NULL,
    [MessageId] int NOT NULL,
    [ConversationId] int NOT NULL,
    [RequestId] int NULL,
    [OriginalFileName] nvarchar(255) NOT NULL,
    [StoredFileName] nvarchar(255) NOT NULL,
    [RelativeFilePath] nvarchar(500) NOT NULL,
    [FileExtension] nvarchar(10) NOT NULL,
    [ContentType] nvarchar(100) NOT NULL,
    [FileSize] bigint NOT NULL,
    [UploadedByNic] nvarchar(20) NOT NULL,
    [UploadedDate] datetime2 NOT NULL,
    [IsSupportingDocument] bit NOT NULL,
    [IsAddedToRequest] bit NOT NULL,
    [AddedToRequestDate] datetime2 NULL,
    [AddedToRequestByNic] nvarchar(20) NULL,
    [ExistingRequestAttachmentId] int NULL,
    [IsActive] bit NOT NULL CONSTRAINT [DF_Chat_Attachments_IsActive] DEFAULT 1,
    CONSTRAINT [PK_Chat_Attachments] PRIMARY KEY ([AttachmentId]),
    CONSTRAINT [FK_Chat_Attachments_Chat_Messages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [Chat_Messages] ([MessageId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Chat_Attachments_Chat_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Chat_Conversations] ([ConversationId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Chat_Attachments_Requests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [Requests] ([RequestId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Chat_Attachments_Request_Attachments_ExistingRequestAttachmentId] FOREIGN KEY ([ExistingRequestAttachmentId]) REFERENCES [Request_Attachments] ([AttachmentId]) ON DELETE NO ACTION
);

-- Indexes for Chat_Attachments
CREATE INDEX [IX_Chat_Attachments_MessageId] ON [Chat_Attachments] ([MessageId]);
CREATE INDEX [IX_Chat_Attachments_ConversationId] ON [Chat_Attachments] ([ConversationId]);
CREATE INDEX [IX_Chat_Attachments_RequestId] ON [Chat_Attachments] ([RequestId]);
CREATE INDEX [IX_Chat_Attachments_UploadedByNic] ON [Chat_Attachments] ([UploadedByNic]);
CREATE INDEX [IX_Chat_Attachments_IsSupportingDocument] ON [Chat_Attachments] ([IsSupportingDocument]);
CREATE INDEX [IX_Chat_Attachments_IsAddedToRequest] ON [Chat_Attachments] ([IsAddedToRequest]);
