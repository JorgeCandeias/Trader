/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/


/* populate the symbol table with the existing symbols in the order table */
WITH [Source] AS
(
    SELECT DISTINCT [Symbol] FROM [dbo].[Order]
)
MERGE INTO [dbo].[Symbol] AS [T]
USING [Source] AS [S]
ON [S].[Symbol] = [T].[Name]
WHEN NOT MATCHED BY TARGET THEN
INSERT ([Name])
VALUES ([Symbol])
;
GO

/* populate the ids in the order table with ids now populated in the symbol table */
WITH [Source] AS
(
    SELECT
        [Id],
        [Name]
    FROM
        [dbo].[Symbol]
)
MERGE INTO [dbo].[Order] AS [T]
USING [Source] AS [S]
ON [S].[Name] = [T].[Symbol] AND [T].[SymbolId] = 0
WHEN MATCHED THEN
UPDATE SET
    [SymbolId] = [S].[Id]
;
GO
