CREATE PROCEDURE [dbo].[SetKline]
	@SymbolId INT,
	@Interval INT,
	@OpenTime DATETIME2(7),
	@CloseTime DATETIME2(7),
	@EventTime DATETIME2(7),
	@FirstTradeId BIGINT,
	@LastTradeId BIGINT,
	@OpenPrice DECIMAL (18,8),
	@HighPrice DECIMAL (18,8),
	@LowPrice DECIMAL (18,8),
	@ClosePrice DECIMAL (18,8),
	@Volume DECIMAL (18,8),
	@QuoteAssetVolume DECIMAL (18,8),
	@TradeCount INT,
	@IsClosed BIT,
	@TakerBuyBaseAssetVolume DECIMAL (18,8),
	@TakerBuyQuoteAssetVolume DECIMAL (18,8)
AS

SET NOCOUNT ON;

WITH [Source] AS
(
	SELECT
		@SymbolId AS [SymbolId],
		@Interval AS [Interval],
		@OpenTime AS [OpenTime],
		@CloseTime AS [CloseTime],
		@EventTime AS [EventTime],
		@FirstTradeId AS [FirstTradeId],
		@LastTradeId AS [LastTradeId],
		@OpenPrice AS [OpenPrice],
		@HighPrice AS [HighPrice],
		@LowPrice AS [LowPrice],
		@ClosePrice AS [ClosePrice],
		@Volume AS [Volume],
		@QuoteAssetVolume AS [QuoteAssetVolume],
		@TradeCount AS [TradeCount],
		@IsClosed AS [IsClosed],
		@TakerBuyBaseAssetVolume AS [TakerBuyBaseAssetVolume],
		@TakerBuyQuoteAssetVolume AS [TakerBuyQuoteAssetVolume]
)

MERGE INTO [dbo].[Kline] AS [T]
USING [Source] AS [S]
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