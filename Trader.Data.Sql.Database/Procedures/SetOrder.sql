CREATE PROCEDURE [dbo].[SetOrder]
    @Symbol NVARCHAR(100),
    @OrderId BIGINT,
    @OrderListId BIGINT,
    @ClientOrderId NVARCHAR(100),
    @Price DECIMAL (18,8),
    @OriginalQuantity DECIMAL (18,8),
    @ExecutedQuantity DECIMAL (18,8),
    @CummulativeQuoteQuantity DECIMAL (18,8),
    @OriginalQuoteOrderQuantity DECIMAL (18,8),
    @Status INT,
    @TimeInForce INT,
    @Type INT,
    @Side INT,
    @StopPrice DECIMAL (18,8),
    @IcebergQuantity DECIMAL (18,8),
    @Time DATETIME2(7),
    @UpdateTime DATETIME2(7),
    @IsWorking BIT
AS

SET XACT_ABORT ON;
SET NOCOUNT ON;

DECLARE @SymbolId INT;
EXECUTE [dbo].[GetOrAddSymbol] @Name = @Symbol, @Id = @SymbolId OUT;

WITH [Source] AS
(
    SELECT
        @SymbolId AS [SymbolId],
        @OrderId AS [OrderId],
        @OrderListId AS [OrderListId],
        @ClientOrderId AS [ClientOrderId],
        @Price AS [Price],
        @OriginalQuantity AS [OriginalQuantity],
        @ExecutedQuantity AS [ExecutedQuantity],
        @CummulativeQuoteQuantity AS [CummulativeQuoteQuantity],
        @OriginalQuoteOrderQuantity AS [OriginalQuoteOrderQuantity],
        @Status AS [Status],
        @TimeInForce AS [TimeInForce],
        @Type AS [Type],
        @Side AS [Side],
        @StopPrice AS [StopPrice],
        @IcebergQuantity AS [IcebergQuantity],
        @Time AS [Time],
        @UpdateTime AS [UpdateTime],
        @IsWorking AS [IsWorking]
)
MERGE INTO [dbo].[Order] WITH (UPDLOCK, HOLDLOCK) AS [T]
USING [Source] AS [S]
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
