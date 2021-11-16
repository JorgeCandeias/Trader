CREATE PROCEDURE [dbo].[GetOrAddSymbol]
	@Name NVARCHAR(100),
	@Id INT OUTPUT
AS

/* clear the input just in case anything was passed in */
SET @Id = NULL;

/* quick path for existing symbol */
SELECT @Id = [Id] FROM [dbo].[Symbol] WHERE [Name] = @Name;
IF @Id IS NOT NULL RETURN;

/* slow path for adding a new symbol */
WITH [Source] AS
(
	SELECT
		@Name AS [Name]
)
MERGE INTO [dbo].[Symbol] WITH (UPDLOCK, HOLDLOCK) AS [T]
USING [Source] AS [S]
ON [S].[Name] = [T].[Name]
WHEN NOT MATCHED BY TARGET THEN
INSERT ([Name])
VALUES ([Name])
;

SELECT @Id = [Id] FROM [dbo].[Symbol] WHERE [Name] = @Name;
RETURN;

GO