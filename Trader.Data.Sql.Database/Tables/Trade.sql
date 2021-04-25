CREATE TABLE [dbo].[Trade]
(
	[Symbol] NVARCHAR(100) NOT NULL,
    [Id] BIGINT NOT NULL,
    [OrderId] BIGINT NOT NULL,
    [OrderListId] BIGINT NOT NULL,
    [Price] DECIMAL (18,8) NOT NULL,
    [Quantity] DECIMAL (18,8) NOT NULL,
    [QuoteQuantity] DECIMAL (18,8) NOT NULL,
    [Commission] DECIMAL (18,8) NOT NULL,
    [CommissionAsset] NVARCHAR(100) NOT NULL,
    [Time] DATETIME2(7) NOT NULL,
    [IsBuyer] BIT NOT NULL,
    [IsMaker] BIT NOT NULL,
    [IsBestMatch] BIT NOT NULL,

    CONSTRAINT [PK_Trade] PRIMARY KEY CLUSTERED
    (
        [Symbol],
        [Id]
    )
)
GO

CREATE NONCLUSTERED INDEX [NCI_Trade_Symbol_OrderId]
ON [dbo].[Trade]
(
    [Symbol],
    [OrderId]
)
GO