-- =========================================================================
-- Database Manual Update Script
-- Description:
--   1. Adds [IsActive] column (bit, default 1) to [Citizen_Welfare_Status]
--   2. Safely drops [citizen_special_statuses] table
--   3. Inserts [newFamilyHead] dropdown config into [Service_Form_Schema]
-- =========================================================================

-- 1. Add IsActive column to Citizen_Welfare_Status if it does not exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Citizen_Welfare_Status]') AND name = N'IsActive')
BEGIN
    ALTER TABLE [Citizen_Welfare_Status] ADD [IsActive] bit NOT NULL DEFAULT 1;
    PRINT 'Added IsActive column to Citizen_Welfare_Status table.';
END
ELSE
BEGIN
    PRINT 'IsActive column already exists in Citizen_Welfare_Status table.';
END
GO

-- 2. Safely Drop citizen_special_statuses table if it exists
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[citizen_special_statuses]') AND type in (N'U'))
BEGIN
    DROP TABLE [citizen_special_statuses];
    PRINT 'Dropped citizen_special_statuses table.';
END
ELSE
BEGIN
    PRINT 'citizen_special_statuses table does not exist or has already been dropped.';
END
GO

-- 3. Insert the newFamilyHead field for Category 3 (Remove Deceased Family Member) if it does not exist
IF NOT EXISTS (SELECT * FROM [Service_Form_Schema] WHERE [CategoryId] = 3 AND [FieldName] = 'newFamilyHead')
BEGIN
    INSERT INTO [Service_Form_Schema] (CategoryId, FieldName, FieldLabel, FieldType, DropdownOptions, IsRequired, ValidationRules, DisplayOrder)
    VALUES (3, 'newFamilyHead', 'New Family Head', 'dropdown', NULL, 0, '{"dataSource": "FamilyMembers", "filterCriteria": {"minAge": 18}, "displayField": "fullName", "valueField": "nic"}', 14);
    PRINT 'Inserted newFamilyHead field into Service_Form_Schema for Category 3.';
END
ELSE
BEGIN
    PRINT 'newFamilyHead field already exists in Service_Form_Schema for Category 3.';
END
GO
