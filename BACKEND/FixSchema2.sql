UPDATE [dbo].[Service_Form_Schema] 
SET ValidationRules = REPLACE(ValidationRules, '"displayField": "fullName-nic"', '"displayField": "fullName"') 
WHERE CategoryId = 10 AND FieldName = 'childSelection';
