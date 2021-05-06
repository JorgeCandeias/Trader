CREATE PROCEDURE [dbo].[GetMaxTradeId]
	@Symbol NVARCHAR(100)
AS

SET NOCOUNT ON;

SELECT
	MAX([T].[Id]) AS [Id]
FROM
	[dbo].[Trade] AS [T]
	INNER JOIN [dbo].[Symbol] AS [S]
		ON [S].[Id] = [T].[SymbolId]
WHERE
	[S].[Name] = @Symbol

RETURN 0
GO