CREATE PROCEDURE [dbo].[GetTrades]
	@Symbol NVARCHAR(100)
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

RETURN 0
GO