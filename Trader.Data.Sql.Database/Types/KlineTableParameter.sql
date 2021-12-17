CREATE TYPE [dbo].[KlineTableParameter] AS TABLE
(
	[SymbolId] INT NOT NULL,
	[Interval] INT,
	[OpenTime] DATETIME2(7),
	[CloseTime] DATETIME2(7),
	[EventTime] DATETIME2(7),
	[FirstTradeId] BIGINT,
	[LastTradeId] BIGINT,
	[OpenPrice] DECIMAL (28,8),
	[HighPrice] DECIMAL (28,8),
	[LowPrice] DECIMAL (28,8),
	[ClosePrice] DECIMAL (28,8),
	[Volume] DECIMAL (28,8),
	[QuoteAssetVolume] DECIMAL (28,8),
	[TradeCount] INT,
	[IsClosed] BIT,
	[TakerBuyBaseAssetVolume] DECIMAL (28,8),
	[TakerBuyQuoteAssetVolume] DECIMAL (28,8)
)
GO