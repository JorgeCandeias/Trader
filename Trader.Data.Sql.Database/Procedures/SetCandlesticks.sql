CREATE PROCEDURE [dbo].[SetCandlesticks]
	@Candlesticks [dbo].[CandlestickTableParameter] READONLY
AS

SET NOCOUNT ON;

MERGE INTO [dbo].[Candlestick] AS [T]
USING @Candlesticks AS [S]
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
	[OpenPrice],
	[HighPrice],
	[LowPrice],
	[ClosePrice],
	[Volume],
	[QuoteAssetVolume],
	[TradeCount],
	[TakerBuyBaseAssetVolume],
	[TakerBuyQuoteAssetVolume]
)
VALUES
(
	[SymbolId],
	[Interval],
	[OpenTime],
	[CloseTime],
	[OpenPrice],
	[HighPrice],
	[LowPrice],
	[ClosePrice],
	[Volume],
	[QuoteAssetVolume],
	[TradeCount],
	[TakerBuyBaseAssetVolume],
	[TakerBuyQuoteAssetVolume]
)
WHEN MATCHED AND [S].[EventTime] >= [T].[EventTime] THEN
UPDATE SET
	[CloseTime] = [S].[CloseTime],
	[OpenPrice] = [S].[OpenPrice],
	[HighPrice] = [S].[HighPrice],
	[LowPrice] = [S].[LowPrice],
	[ClosePrice] = [S].[ClosePrice],
	[Volume] = [S].[Volume],
	[QuoteAssetVolume] = [S].[QuoteAssetVolume],
	[TradeCount] = [S].[TradeCount],
	[TakerBuyBaseAssetVolume] = [S].[TakerBuyBaseAssetVolume],
	[TakerBuyQuoteAssetVolume] = [S].[TakerBuyQuoteAssetVolume]
;

RETURN 0
GO