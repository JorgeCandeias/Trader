CREATE PROCEDURE [dbo].[SetTrades]
	@Trades [dbo].[TradeTableParameter] READONLY
AS

SET NOCOUNT ON;

MERGE INTO [dbo].[Trade] AS [T]
USING @Trades AS [S]
ON [S].[SymbolId] = [T].[SymbolId]
AND [S].[Id] = [T].[Id]
WHEN NOT MATCHED BY TARGET THEN
INSERT
(
    [SymbolId],
    [Id],
    [OrderId],
    [OrderListId],
    [Price],
    [Quantity],
    [QuoteQuantity],
    [Commission],
    [CommissionAsset],
    [Time],
    [IsBuyer],
    [IsMaker],
    [IsBestMatch]
)
VALUES
(
    [SymbolId],
    [Id],
    [OrderId],
    [OrderListId],
    [Price],
    [Quantity],
    [QuoteQuantity],
    [Commission],
    [CommissionAsset],
    [Time],
    [IsBuyer],
    [IsMaker],
    [IsBestMatch]    
)
WHEN MATCHED THEN
UPDATE SET
    [OrderId] = [S].[OrderId],
    [OrderListId] = [S].[OrderListId],
    [Price] = [S].[Price],
    [Quantity] = [S].[Quantity],
    [QuoteQuantity] = [S].[QuoteQuantity],
    [Commission] = [S].[Commission],
    [CommissionAsset] = [S].[CommissionAsset],
    [Time] = [S].[Time],
    [IsBuyer] = [S].[IsBuyer],
    [IsMaker] = [S].[IsMaker],
    [IsBestMatch] = [S].[IsBestMatch]
;

RETURN 0
GO