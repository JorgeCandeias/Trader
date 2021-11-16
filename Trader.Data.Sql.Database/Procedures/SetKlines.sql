CREATE PROCEDURE [dbo].[SetKlines]
	@Klines [dbo].[KlineTableParameter] READONLY
AS

SET XACT_ABORT ON;
SET NOCOUNT ON;

MERGE INTO [dbo].[Kline] WITH (UPDLOCK, HOLDLOCK) AS [T]
USING @Klines AS [S]
ON [T].[SymbolId] = [S].[SymbolId]
AND [T].[Interval] = [S].[Interval]
AND [T].[OpenTime] = [S].[OpenTime]
WHEN NOT MATCHED BY TARGET THEN
INSERT
(
	[SymbolId],
	[Interval],
	[OpenTime],
	[CloseTime],
	[EventTime],
	[FirstTradeId],
	[LastTradeId],
	[OpenPrice],
	[HighPrice],
	[LowPrice],
	[ClosePrice],
	[Volume],
	[QuoteAssetVolume],
	[TradeCount],
	[IsClosed],
	[TakerBuyBaseAssetVolume],
	[TakerBuyQuoteAssetVolume]
)
VALUES
(
	[SymbolId],
	[Interval],
	[OpenTime],
	[CloseTime],
	[EventTime],
	[FirstTradeId],
	[LastTradeId],
	[OpenPrice],
	[HighPrice],
	[LowPrice],
	[ClosePrice],
	[Volume],
	[QuoteAssetVolume],
	[TradeCount],
	[IsClosed],
	[TakerBuyBaseAssetVolume],
	[TakerBuyQuoteAssetVolume]
)
WHEN MATCHED AND [S].[EventTime] >= [T].[EventTime] THEN
UPDATE SET
	[CloseTime] = [S].[CloseTime],
	[OpenPrice] = [S].[OpenPrice],
	[HighPrice] = [S].[HighPrice],
	[EventTime] = [S].[EventTime],
	[FirstTradeId] = [S].[FirstTradeId],
	[LastTradeId] = [S].[LastTradeId],
	[LowPrice] = [S].[LowPrice],
	[ClosePrice] = [S].[ClosePrice],
	[Volume] = [S].[Volume],
	[QuoteAssetVolume] = [S].[QuoteAssetVolume],
	[TradeCount] = [S].[TradeCount],
	[IsClosed] = [S].[IsClosed],
	[TakerBuyBaseAssetVolume] = [S].[TakerBuyBaseAssetVolume],
	[TakerBuyQuoteAssetVolume] = [S].[TakerBuyQuoteAssetVolume]
;

RETURN 0
GO