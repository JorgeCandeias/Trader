﻿CREATE TABLE [dbo].[Ticker]
(
	[SymbolId] INT NOT NULL,
	[EventTime] DATETIME2(7) NOT NULL,
	[ClosePrice] DECIMAL(18,8) NOT NULL,
	[OpenPrice] DECIMAL(18,8) NOT NULL,
	[HighPrice] DECIMAL(18,8) NOT NULL,
	[LowPrice] DECIMAL(18,8) NOT NULL,
	[AssetVolume] DECIMAL(18,8) NOT NULL,
	[QuoteVolume] DECIMAL(18,8) NOT NULL,

	CONSTRAINT [PK_Ticker] PRIMARY KEY CLUSTERED
	(
		[SymbolId]
	)
)
GO
