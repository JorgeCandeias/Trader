CREATE PROCEDURE [dbo].[SetOrder]
	@Symbol NVARCHAR(100) NOT NULL,
    @OrderId BIGINT NOT NULL,
    @OrderListId BIGINT NOT NULL,
    @ClientOrderId NVARCHAR(100) NOT NULL,
    @Price DECIMAL (18,8) NOT NULL,
    @OriginalQuantity DECIMAL (18,8) NOT NULL,
    @ExecutedQuantity DECIMAL (18,8) NOT NULL,
    @CummulativeQuoteQuantity DECIMAL (18,8) NOT NULL,
    @OriginalQuoteOrderQuantity DECIMAL (18,8) NOT NULL,
    @Status INT NOT NULL,
    @TimeInForce INT NOT NULL,
    @Type INT NOT NULL,
    @Side INT NOT NULL,
    @StopPrice DECIMAL (18,8) NOT NULL,
    @IcebergQuantity DECIMAL (18,8) NOT NULL,
    @Time DATETIME2(7) NOT NULL,
    @UpdateTime DATETIME2(7) NOT NULL,
    @IsWorking BIT NOT NULL
AS

WITH [Source] AS
(
    SELECT
        @Symbol AS [Symbol],
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
MERGE INTO [dbo].[Order] AS [T]
USING [Source] AS [S]
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
WHEN MATCHED THEN
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
