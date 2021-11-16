CREATE PROCEDURE [dbo].[SetOrders]
    @Orders [dbo].[OrderTableParameter] READONLY
AS

SET XACT_ABORT ON;
SET NOCOUNT ON;

MERGE INTO [dbo].[Order] WITH (UPDLOCK, HOLDLOCK) AS [T]
USING @Orders AS [S]
    ON [S].[SymbolId] = [T].[SymbolId]
    AND [S].[OrderId] = [T].[OrderId]
WHEN NOT MATCHED BY TARGET THEN
    INSERT
    (
        [SymbolId],
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
        [SymbolId],
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
WHEN MATCHED AND [T].[IsTransient] = 1 AND [S].[UpdateTime] >= [T].[UpdateTime] THEN
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
