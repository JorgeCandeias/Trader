CREATE PROCEDURE [dbo].[GetBalance]
	@Asset NVARCHAR(100)
AS

SET NOCOUNT ON;

SELECT
	[Asset],
	[Free],
	[Locked],
	[UpdatedTime]
FROM
	[dbo].[Balance]
WHERE
	[Asset] = @Asset

RETURN 0
GO
