CREATE PROCEDURE [dbo].[GetKlines]
	@Symbol NVARCHAR(100),
	@Interval INT,
	@StartOpenTime DATETIME2(7),
	@EndOpenTime DATETIME2(7)
AS

SELECT
	[S].[Name] AS [Symbol],
	[K].[Interval],
	[K].[OpenTime],
	[K].[CloseTime],
	[K].[EventTime],
	[K].[FirstTradeId],
	[K].[LastTradeId],
	[K].[OpenPrice],
	[K].[HighPrice],
	[K].[LowPrice],
	[K].[ClosePrice],
	[K].[Volume],
	[K].[QuoteAssetVolume],
	[K].[TradeCount],
	[K].[IsClosed],
	[K].[TakerBuyBaseAssetVolume],
	[K].[TakerBuyQuoteAssetVolume]
FROM
	[dbo].[Kline] AS [K]
	INNER JOIN [dbo].[Symbol] AS [S]
		ON [S].[Id] = [K].[SymbolId]
WHERE
	[S].[Name] = @Symbol
	AND [K].[Interval] = @Interval
	AND [K].[OpenTime] BETWEEN @StartOpenTime AND @EndOpenTime
ORDER BY
	[Interval],
	[OpenTime]

RETURN 0
GO
