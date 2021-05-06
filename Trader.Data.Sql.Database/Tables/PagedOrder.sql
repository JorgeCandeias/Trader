CREATE TABLE [dbo].[PagedOrder]
(
	[SymbolId] INT NOT NULL,
	[OrderId] BIGINT NOT NULL,

	CONSTRAINT [PK_PagedOrder] PRIMARY KEY CLUSTERED
	(
		[SymbolId]
	)
)
