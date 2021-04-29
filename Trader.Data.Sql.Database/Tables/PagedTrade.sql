CREATE TABLE [dbo].[PagedTrade]
(
	[Symbol] NVARCHAR(100) NOT NULL,
	[TradeId] BIGINT NOT NULL,

	CONSTRAINT [PK_PagedTrade] PRIMARY KEY CLUSTERED
	(
		[Symbol]
	)
)
