CREATE PROCEDURE [dbo].[GetOrAddSymbol]
	@Name NVARCHAR(100),
	@Id INT OUTPUT
AS

/* quick path for existing symbol */
SELECT @Id = [Id] FROM [Symbol] WHERE [Name] = @Name;
IF @Id IS NOT NULL RETURN 0;

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

SELECT @Id = [Id] FROM [Symbol] WHERE [Name] = @Name;
RETURN 0;

GO