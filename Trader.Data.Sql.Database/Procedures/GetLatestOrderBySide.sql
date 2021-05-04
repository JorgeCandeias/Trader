CREATE PROCEDURE [dbo].[GetLatestOrderBySide]
	@Symbol NVARCHAR(100),
    @Side INT
AS

SET NOCOUNT ON;

SELECT TOP (1)
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
    AND [O].[Side] = @Side
ORDER BY
    [O].[OrderId] DESC

RETURN 0
GO
