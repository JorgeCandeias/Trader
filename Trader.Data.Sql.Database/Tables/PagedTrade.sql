CREATE TABLE [dbo].[PagedTrade]
(
	[SymbolId] INT NOT NULL,
	[TradeId] BIGINT NOT NULL,

	CONSTRAINT [PK_PagedTrade] PRIMARY KEY CLUSTERED
	(
		[SymbolId]
	)
)
