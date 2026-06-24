DECLARE @tables TABLE (TableName NVARCHAR(128));
INSERT INTO @tables (TableName) VALUES 
('announcement_target_citizens'), ('announcements'), ('appointments'), 
('chat_attachments'), ('chat_messages'), ('conversations'), 
('document_categories'), ('download_forms'), ('gn_leaves_holidays'), 
('notifications'), ('poll_options'), ('poll_votes'), 
('request_attachments'), ('request_remarks'), ('request_timelines'), 
('service_requests'), ('survey_responses'), ('transfer_requests');

DECLARE @sql NVARCHAR(MAX) = '';

-- Get all DISTINCT foreign keys that either point TO the tables or FROM the tables
SELECT @sql += 'ALTER TABLE ' + QUOTENAME(fk.TABLE_SCHEMA) + '.' + QUOTENAME(fk.TABLE_NAME) + ' DROP CONSTRAINT ' + QUOTENAME(fk.CONSTRAINT_NAME) + ';' + CHAR(13)
FROM (
    SELECT DISTINCT rc.CONSTRAINT_NAME, fk_col.TABLE_SCHEMA, fk_col.TABLE_NAME
    FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE fk_col ON rc.CONSTRAINT_NAME = fk_col.CONSTRAINT_NAME
    WHERE rc.UNIQUE_CONSTRAINT_NAME IN (
        SELECT CONSTRAINT_NAME
        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE TABLE_NAME IN (SELECT TableName FROM @tables)
    )
    OR fk_col.TABLE_NAME IN (SELECT TableName FROM @tables)
) fk;

PRINT 'Executing FK Drops:';
PRINT @sql;
IF @sql <> '' EXEC sp_executesql @sql;

-- Drop the tables themselves
SET @sql = '';
SELECT @sql += 'DROP TABLE IF EXISTS ' + QUOTENAME(TABLE_SCHEMA) + '.' + QUOTENAME(TABLE_NAME) + ';' + CHAR(13)
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME IN (SELECT TableName FROM @tables) AND TABLE_TYPE = 'BASE TABLE';

PRINT 'Executing Table Drops:';
PRINT @sql;
IF @sql <> '' EXEC sp_executesql @sql;

PRINT 'Cleanup successful.';
