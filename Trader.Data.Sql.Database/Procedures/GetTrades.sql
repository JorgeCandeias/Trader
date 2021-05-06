CREATE PROCEDURE [dbo].[GetTrades]
	@Symbol NVARCHAR(100)
AS

SET NOCOUNT ON;

SELECT
	[S].[Name] AS [Symbol],
    [T].[Id],
    [T].[OrderId],
    [T].[OrderListId],
    [T].[Price],
    [T].[Quantity],
    [T].[QuoteQuantity],
    [T].[Commission],
    [T].[CommissionAsset],
    [T].[Time],
    [T].[IsBuyer],
    [T].[IsMaker],
    [T].[IsBestMatch]
FROM
	[dbo].[Trade] AS [T]
    INNER JOIN [dbo].[Symbol] AS [S]
        ON [S].[Id] = [T].[SymbolId]
WHERE
    [S].[Name] = @Symbol

RETURN 0
GO