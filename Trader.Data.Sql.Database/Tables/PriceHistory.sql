CREATE TABLE [dbo].[PriceHistory]
(
	[SymbolId] INT NOT NULL,
	[Interval] INT,
	[OpenTime] DATETIME2(7),
	[CloseTime] DATETIME2(7),
	[OpenPrice] DECIMAL (18,8),
	[HighPrice] DECIMAL (18,8),
	[LowPrice] DECIMAL (18,8),
	[ClosePrice] DECIMAL (18,8),
	[Volume] DECIMAL (18,8),
	[QuoteAssetVolume] DECIMAL (18,8),
	[TradeCount] INT,
	[TakerBuyBaseAssetVolume] DECIMAL (18,8),
	[TakerBuyQuoteAssetVolume] DECIMAL (18,8),

	CONSTRAINT [PK_Candlestick] PRIMARY KEY CLUSTERED
	(
		[SymbolId],
		[Interval],
		[OpenTime]
	)
)
GO
