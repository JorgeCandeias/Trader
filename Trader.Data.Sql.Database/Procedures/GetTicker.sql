CREATE PROCEDURE [dbo].[GetTicker]
	@Symbol NVARCHAR(100)
AS

SET NOCOUNT ON;

SELECT
	[S].[Name] AS [Symbol],
	[EventTime],
	[ClosePrice],
	[OpenPrice],
	[HighPrice],
	[LowPrice],
	[AssetVolume],
	[QuoteVolume]
FROM
	[dbo].[Ticker] AS [T]
	INNER JOIN [dbo].[Symbol] AS [S]
		ON [S].[Id] = [T].[SymbolId]
WHERE
	[S].[Name] = @Symbol

RETURN 0
GO