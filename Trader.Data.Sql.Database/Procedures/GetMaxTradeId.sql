CREATE PROCEDURE [dbo].[GetMaxTradeId]
	@Symbol NVARCHAR(100)
AS

SET NOCOUNT ON;

SELECT
	MAX([Id]) AS [Id]
FROM
	[dbo].[Trade]
WHERE
	[Symbol] = @Symbol

RETURN 0
