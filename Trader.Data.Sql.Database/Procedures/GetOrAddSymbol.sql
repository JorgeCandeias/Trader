CREATE PROCEDURE [dbo].[GetOrAddSymbol]
	@Name NVARCHAR(100)
AS

DECLARE @Id INT;

/* quick path for existing symbol */
SELECT @Id = [Id] FROM [dbo].[Symbol] WHERE [Name] = @Name;
IF @Id IS NOT NULL
BEGIN
	SELECT @Id AS [Id];
	RETURN 0;
END;

/* slow path for adding a new symbol */
WITH [Source] AS
(
	SELECT
		@Name AS [Name]
)
MERGE INTO [dbo].[Symbol] AS [T]
USING [Source] AS [S]
ON [S].[Name] = [T].[Name]
WHEN NOT MATCHED BY TARGET THEN
INSERT ([Name])
VALUES ([Name])
;

SELECT [Id] FROM [dbo].[Symbol] WHERE [Name] = @Name;
RETURN 0;

GO