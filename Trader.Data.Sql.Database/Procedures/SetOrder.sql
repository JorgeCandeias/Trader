﻿CREATE PROCEDURE [dbo].[SetOrder]
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

SET NOCOUNT ON;

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
WHEN MATCHED AND [S].[Time] > [T].[Time] THEN
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
