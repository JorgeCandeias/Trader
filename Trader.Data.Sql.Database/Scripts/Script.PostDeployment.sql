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



WITH [Source] AS
(
    SELECT
        [Id],
        [Name]
    FROM
        [dbo].[Symbol]
)
MERGE INTO [dbo].[PagedOrder] AS [T]
USING [Source] AS [S]
ON [S].[Name] = [T].[Symbol]
WHEN MATCHED AND [SymbolId] = 0 THEN
UPDATE SET
    [SymbolId] = [S].[Id]
;
GO

ALTER DATABASE [$(DatabaseName)] SET QUERY_STORE CLEAR
GO