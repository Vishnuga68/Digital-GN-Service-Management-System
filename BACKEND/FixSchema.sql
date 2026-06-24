UPDATE [dbo].[Service_Form_Schema] 
SET ValidationRules = REPLACE(ValidationRules, '"value": "Child under 18"', '"value": "child"') 
WHERE CategoryId = 10 AND FormSection = 'Personal Information';
