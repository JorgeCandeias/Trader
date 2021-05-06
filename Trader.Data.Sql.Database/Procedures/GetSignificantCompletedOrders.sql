CREATE PROCEDURE [dbo].[GetSignificantCompletedOrders]
	@Symbol NVARCHAR(100)
AS

SET NOCOUNT ON;

SELECT
	[S].[Name] AS [Symbol],
    [O].[OrderId],
    [O].[OrderListId],
    [O].[ClientOrderId],
    [O].[Price],
    [O].[OriginalQuantity],
    [O].[ExecutedQuantity],
    [O].[CummulativeQuoteQuantity],
    [O].[OriginalQuoteOrderQuantity],
    [O].[Status],
    [O].[TimeInForce],
    [O].[Type],
    [O].[Side],
    [O].[StopPrice],
    [O].[IcebergQuantity],
    [O].[Time],
    [O].[UpdateTime],
    [O].[IsWorking]
FROM
	[dbo].[Order] AS [O]
    INNER JOIN [dbo].[Symbol] AS [S]
        ON [S].[Id] = [O].[SymbolId]
WHERE
	[S].[Name] = @Symbol
    AND [IsTransient] = 0
    AND [ExecutedQuantity] > 0.0

RETURN 0
GO
