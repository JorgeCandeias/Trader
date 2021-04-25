CREATE PROCEDURE [dbo].[GetOrders]
	@Symbol NVARCHAR(100),
    @Side INT,
    @Significant BIT,
    @Transient BIT
AS

SET NOCOUNT ON;

SELECT
	[Symbol],
    [OrderId],
    [OrderListId],
    [ClientOrderId],
    [Price],
    [OriginalQuantity],
    [ExecutedQuantity],
    [CummulativeQuoteQuantity],
    [OriginalQuoteOrderQuantity],
    [Status],
    [TimeInForce],
    [Type],
    [Side],
    [StopPrice],
    [IcebergQuantity],
    [Time],
    [UpdateTime],
    [IsWorking]
FROM
	[dbo].[Order]
WHERE
	[Symbol] = @Symbol
    AND (@Side IS NULL OR [Side] = @Side)
    AND (@Significant IS NULL OR (@Significant = 1 AND [ExecutedQuantity] > 0) OR (@Significant = 0 AND [ExecutedQuantity] <= 0))
    AND (@Transient IS NULL OR (@Transient = 1 AND [Status] IN (1, 2, 5)) OR (@Transient = 0 AND [Status] IN (3, 4, 6, 7)))
OPTION
(
    RECOMPILE
)

RETURN 0
GO
