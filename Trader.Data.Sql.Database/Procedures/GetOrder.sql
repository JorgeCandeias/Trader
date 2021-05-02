CREATE PROCEDURE [dbo].[GetOrder]
	@Symbol NVARCHAR(100),
    @OrderId BIGINT
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
    AND [OrderId] = @OrderId

RETURN 0
GO
