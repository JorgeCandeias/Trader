﻿CREATE TYPE [dbo].[TickerTableParameter] AS TABLE
(
	[SymbolId] INT NOT NULL,
	[EventTime] DATETIME2(7) NOT NULL,
	[ClosePrice] DECIMAL(18,8) NOT NULL,
	[OpenPrice] DECIMAL(18,8) NOT NULL,
	[HighPrice] DECIMAL(18,8) NOT NULL,
	[LowPrice] DECIMAL(18,8) NOT NULL,
	[AssetVolume] DECIMAL(18,8) NOT NULL,
	[QuoteVolume] DECIMAL(18,8) NOT NULL
)
GO