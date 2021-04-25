CREATE PROCEDURE [dbo].[GetNonSignificantTransientOrdersBySide]
	@Symbol NVARCHAR(100),
    @Side INT
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
    AND [Side] = @Side
    AND [IsTransient] = 1
    AND [ExecutedQuantity] <= 0

RETURN 0
GO
