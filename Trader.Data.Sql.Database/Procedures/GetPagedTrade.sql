CREATE PROCEDURE [dbo].[GetPagedTrade]
	@Symbol NVARCHAR(100)
AS

SET NOCOUNT ON;

SELECT
	[TradeId]
FROM
	[dbo].[PagedTrade] AS [PT]
	INNER JOIN [dbo].[Symbol] AS [S]
		ON [S].[Id] = [PT].[SymbolId]
WHERE
	[S].[Name] = @Symbol

RETURN 0
GO