CREATE TABLE [dbo].[PagedOrder]
(
	[SymbolId] INT NOT NULL,
	[Symbol] NVARCHAR(100) NOT NULL,
	[OrderId] BIGINT NOT NULL,

	CONSTRAINT [PK_PagedOrder] PRIMARY KEY CLUSTERED
	(
		[Symbol]
	)
)
