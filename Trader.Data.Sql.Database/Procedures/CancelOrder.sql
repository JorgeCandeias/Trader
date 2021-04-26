CREATE PROCEDURE [dbo].[CancelOrder]
	@Symbol NVARCHAR(100),
    @OrderId BIGINT,
    @OrderListId BIGINT,
    @ClientOrderId NVARCHAR(100),
    @Price DECIMAL (18,8),
    @OriginalQuantity DECIMAL (18,8),
    @ExecutedQuantity DECIMAL (18,8),
    @CummulativeQuoteQuantity DECIMAL (18,8),
    @Status INT,
    @TimeInForce INT,
    @Type INT,
    @Side INT
AS

SET NOCOUNT ON;

UPDATE [dbo].[Order]
SET
    [OrderListId] = @OrderListId,
    [ClientOrderId] = @ClientOrderId,
    [Price] = @Price,
    [OriginalQuantity] = @OriginalQuantity,
    [ExecutedQuantity] = @ExecutedQuantity,
    [CummulativeQuoteQuantity] = @CummulativeQuoteQuantity,
    [Status] = @Status,
    [TimeInForce] = @TimeInForce,
    [Type] = @Type,
    [Side] = @Side,
    [UpdateTime] = DATEADD(MILLISECOND, 1, [UpdateTime])
WHERE
    [Symbol] = @Symbol
    AND [OrderId] = @OrderId

RETURN 0
GO
