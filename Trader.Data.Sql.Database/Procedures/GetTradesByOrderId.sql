CREATE PROCEDURE [dbo].[GetTradesByOrderId]
	@Symbol NVARCHAR(100),
    @OrderId BIGINT
AS

SET NOCOUNT ON;

SELECT
	[Symbol],
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
FROM
	[dbo].[Trade]
WHERE
	[Symbol] = @Symbol
    AND [OrderId] = @OrderId

RETURN 0
GO