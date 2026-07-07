-- Add foreign key constraint to gn_announcements table
-- linking created_by and modified_by to gn_officers

-- Step 1: Ensure the columns exactly match the data type of gn_officers.Nic
ALTER TABLE gn_announcements
ALTER COLUMN created_by NVARCHAR(20) NULL;

ALTER TABLE gn_announcements
ALTER COLUMN modified_by NVARCHAR(20) NULL;

-- Drop previous wrong constraints if they exist
ALTER TABLE gn_announcements
DROP CONSTRAINT IF EXISTS FK_GnAnnouncements_CreatedBy;

ALTER TABLE gn_announcements
DROP CONSTRAINT IF EXISTS FK_GnAnnouncements_ModifiedBy;

-- Step 2: Add Correct Constraints
ALTER TABLE gn_announcements
ADD CONSTRAINT FK_GnAnnouncements_CreatedBy
FOREIGN KEY (created_by) REFERENCES gn_officers(Nic);

ALTER TABLE gn_announcements
ADD CONSTRAINT FK_GnAnnouncements_ModifiedBy
FOREIGN KEY (modified_by) REFERENCES gn_officers(Nic);
