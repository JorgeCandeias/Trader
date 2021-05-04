CREATE PROCEDURE [dbo].[CancelOrder]
	@SymbolId INT,
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
    [Side] = @Side
WHERE
    [SymbolId] = @SymbolId
    AND [OrderId] = @OrderId
    AND IsTransient = 1

RETURN 0
GO
