CREATE TABLE [dbo].[PagedOrder]
(
	[Symbol] NVARCHAR(100) NOT NULL,
	[OrderId] BIGINT NOT NULL,

	CONSTRAINT [PK_PagedOrder] PRIMARY KEY CLUSTERED
	(
		[Symbol]
	)
)
