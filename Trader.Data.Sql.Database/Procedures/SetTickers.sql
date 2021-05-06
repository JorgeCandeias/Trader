CREATE PROCEDURE [dbo].[SetTickers]
	@Tickers [dbo].[TickerTableParameter] READONLY
AS

SET NOCOUNT ON;

MERGE INTO [dbo].[Ticker] AS [T]
USING @Tickers AS [S]
ON [T].[SymbolId] = [S].[SymbolId]
WHEN NOT MATCHED BY TARGET THEN
INSERT
(
	[SymbolId],
	[EventTime],
	[ClosePrice],
	[OpenPrice],
	[HighPrice],
	[LowPrice],
	[AssetVolume],
	[QuoteVolume]
)
VALUES
(
	[SymbolId],
	[EventTime],
	[ClosePrice],
	[OpenPrice],
	[HighPrice],
	[LowPrice],
	[AssetVolume],
	[QuoteVolume]
)
WHEN MATCHED AND [S].[EventTime] >= [T].[EventTime] THEN
UPDATE SET
	[EventTime] = [S].[EventTime],
	[ClosePrice] = [S].[ClosePrice],
	[OpenPrice] = [S].[OpenPrice],
	[HighPrice] = [S].[HighPrice],
	[LowPrice] = [S].[LowPrice],
	[AssetVolume] = [S].[AssetVolume],
	[QuoteVolume] = [S].[QuoteVolume]
;

RETURN 0
GO