CREATE TABLE [dbo].[Kline]
(
	[SymbolId] INT NOT NULL,
	[Interval] INT NOT NULL,
	[OpenTime] DATETIME2(7) NOT NULL,
	[CloseTime] DATETIME2(7) NOT NULL,
	[EventTime] DATETIME2(7) NOT NULL,
	[FirstTradeId] BIGINT NOT NULL,
	[LastTradeId] BIGINT NOT NULL,
	[OpenPrice] DECIMAL (18,8) NOT NULL,
	[HighPrice] DECIMAL (18,8) NOT NULL,
	[LowPrice] DECIMAL (18,8) NOT NULL,
	[ClosePrice] DECIMAL (18,8) NOT NULL,
	[Volume] DECIMAL (18,8) NOT NULL,
	[QuoteAssetVolume] DECIMAL (18,8) NOT NULL,
	[TradeCount] INT NOT NULL,
	[IsClosed] BIT NOT NULL,
	[TakerBuyBaseAssetVolume] DECIMAL (18,8) NOT NULL,
	[TakerBuyQuoteAssetVolume] DECIMAL (18,8) NOT NULL,

	CONSTRAINT [PK_Candlestick] PRIMARY KEY CLUSTERED
	(
		[SymbolId],
		[Interval],
		[OpenTime]
	)
)
GO
