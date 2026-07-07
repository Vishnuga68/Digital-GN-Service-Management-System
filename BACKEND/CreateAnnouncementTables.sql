-- Announcement Management Module Database Tables

-- Create gn_announcements table
IF OBJECT_ID('dbo.gn_announcements', 'U') IS NULL
BEGIN
    CREATE TABLE gn_announcements (
        AnnouncementId INT IDENTITY(1,1) PRIMARY KEY,
        GnOfficerNic NVARCHAR(20) NOT NULL,
        GnDivisionId NVARCHAR(50) NOT NULL,
        AnnouncementType NVARCHAR(50) NOT NULL, -- 'Public Announcement', 'Poll', 'Survey'
        Title NVARCHAR(255) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        DocumentId INT NULL,
        ExternalSurveyUrl NVARCHAR(MAX) NULL,
        PollQuestion NVARCHAR(MAX) NULL,
        PublishDate DATETIME NOT NULL,
        ExpiryDate DATETIME NOT NULL,
        CreatedAt DATETIME DEFAULT GETDATE(),
        
        CONSTRAINT FK_Announcements_GnOfficers FOREIGN KEY (GnOfficerNic) REFERENCES gn_officers(Nic),
        CONSTRAINT FK_Announcements_GnDivisions FOREIGN KEY (GnDivisionId) REFERENCES gn_divisions(Id),
        CONSTRAINT FK_Announcements_Documents FOREIGN KEY (DocumentId) REFERENCES gn_documents(DocumentId)
    );
END
GO

-- Create gn_poll_options table
IF OBJECT_ID('dbo.gn_poll_options', 'U') IS NULL
BEGIN
    CREATE TABLE gn_poll_options (
        OptionId INT IDENTITY(1,1) PRIMARY KEY,
        AnnouncementId INT NOT NULL,
        OptionText NVARCHAR(255) NOT NULL,
        
        CONSTRAINT FK_PollOptions_Announcements FOREIGN KEY (AnnouncementId) REFERENCES gn_announcements(AnnouncementId)
    );
END
GO

-- Create gn_poll_votes table
IF OBJECT_ID('dbo.gn_poll_votes', 'U') IS NULL
BEGIN
    CREATE TABLE gn_poll_votes (
        VoteId INT IDENTITY(1,1) PRIMARY KEY,
        AnnouncementId INT NOT NULL,
        OptionId INT NOT NULL,
        CitizenNic NVARCHAR(20) NOT NULL,
        VotedAt DATETIME DEFAULT GETDATE(),
        
        CONSTRAINT FK_PollVotes_Announcements FOREIGN KEY (AnnouncementId) REFERENCES gn_announcements(AnnouncementId),
        CONSTRAINT FK_PollVotes_PollOptions FOREIGN KEY (OptionId) REFERENCES gn_poll_options(OptionId),
        CONSTRAINT FK_PollVotes_Citizens FOREIGN KEY (CitizenNic) REFERENCES Citizen_Master(Nic),
        
        -- A citizen can vote only once per poll
        CONSTRAINT UQ_PollVotes_Announcement_Citizen UNIQUE (AnnouncementId, CitizenNic)
    );
END
GO