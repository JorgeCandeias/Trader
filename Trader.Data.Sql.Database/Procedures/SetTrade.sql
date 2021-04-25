CREATE PROCEDURE [dbo].[SetTrade]
	@Symbol NVARCHAR(100),
    @Id BIGINT,
    @OrderId BIGINT,
    @OrderListId BIGINT,
    @Price DECIMAL (18,8),
    @Quantity DECIMAL (18,8),
    @QuoteQuantity DECIMAL (18,8),
    @Commission DECIMAL (18,8),
    @CommissionAsset NVARCHAR(100),
    @Time DATETIME2(7),
    @IsBuyer BIT,
    @IsMaker BIT,
    @IsBestMatch BIT
AS

SET NOCOUNT ON;

WITH Source AS
(
    SELECT
        @Symbol AS [Symbol],
        @Id AS [Id],
        @OrderId AS [OrderId],
        @OrderListId AS [OrderListId],
        @Price AS [Price],
        @Quantity AS [Quantity],
        @QuoteQuantity AS [QuoteQuantity],
        @Commission AS [Commission],
        @CommissionAsset AS [CommissionAsset],
        @Time AS [Time],
        @IsBuyer AS [IsBuyer],
        @IsMaker AS [IsMaker],
        @IsBestMatch AS [IsBestMatch]
)
MERGE INTO [dbo].[Trade] AS [T]
USING [Source] AS [S]
ON [S].[Symbol] = [T].[Symbol]
AND [S].[Id] = [T].[Id]
WHEN NOT MATCHED BY TARGET THEN
INSERT
(
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
)
VALUES
(
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