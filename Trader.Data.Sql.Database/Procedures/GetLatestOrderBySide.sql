CREATE PROCEDURE [dbo].[GetLatestOrderBySide]
	@Symbol NVARCHAR(100),
    @Side INT
AS

SET NOCOUNT ON;

SELECT TOP (1)
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
ORDER BY
    [OrderId] DESC

RETURN 0
GO
