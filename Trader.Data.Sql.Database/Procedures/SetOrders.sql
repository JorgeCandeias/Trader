CREATE PROCEDURE [dbo].[SetOrders]
    @Orders [dbo].[OrderTableParameter] READONLY
AS

SET NOCOUNT ON;

MERGE INTO [dbo].[Order] AS [T]
USING @Orders AS [S]
    ON [S].[Symbol] = [T].[Symbol]
    AND [S].[OrderId] = [T].[OrderId]
WHEN NOT MATCHED BY TARGET THEN
    INSERT
    (
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
    )
    VALUES
    (
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
    )
WHEN MATCHED AND [S].[UpdateTime] > [T].[UpdateTime] THEN
UPDATE SET
    [OrderListId] = [S].[OrderListId],
    [ClientOrderId] = [S].[ClientOrderId],
    [Price] = [S].[Price],
    [OriginalQuantity] = [S].[OriginalQuantity],
    [ExecutedQuantity] = [S].[ExecutedQuantity],
    [CummulativeQuoteQuantity] = [S].[CummulativeQuoteQuantity],
    [OriginalQuoteOrderQuantity] = [S].[OriginalQuoteOrderQuantity],
    [Status] = [S].[Status],
    [TimeInForce] = [S].[TimeInForce],
    [Type] = [S].[Type],
    [Side] = [S].[Side],
    [StopPrice] = [S].[StopPrice],
    [IcebergQuantity] = [S].[IcebergQuantity],
    [Time] = [S].[Time],
    [UpdateTime] = [S].[UpdateTime],
    [IsWorking] = [S].[IsWorking]
;

RETURN 0
GO
