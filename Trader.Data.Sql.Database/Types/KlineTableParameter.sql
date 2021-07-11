CREATE TYPE [dbo].[KlineTableParameter] AS TABLE
(
	[SymbolId] INT NOT NULL,
	[Interval] INT,
	[OpenTime] DATETIME2(7),
	[CloseTime] DATETIME2(7),
	[EventTime] DATETIME2(7),
	[FirstTradeId] BIGINT,
	[LastTradeId] BIGINT,
	[OpenPrice] DECIMAL (18,8),
	[HighPrice] DECIMAL (18,8),
	[LowPrice] DECIMAL (18,8),
	[ClosePrice] DECIMAL (18,8),
	[Volume] DECIMAL (18,8),
	[QuoteAssetVolume] DECIMAL (18,8),
	[TradeCount] INT,
	[IsClosed] BIT,
	[TakerBuyBaseAssetVolume] DECIMAL (18,8),
	[TakerBuyQuoteAssetVolume] DECIMAL (18,8)
)
GO