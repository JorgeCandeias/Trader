CREATE PROCEDURE [dbo].[SetTrade]
	@Symbol NVARCHAR(100) NOT NULL,
    @Id BIGINT NOT NULL,
    @OrderId BIGINT NOT NULL,
    @OrderListId BIGINT NOT NULL,
    @Price DECIMAL (18,8) NOT NULL,
    @Quantity DECIMAL (18,8) NOT NULL,
    @QuoteQuantity DECIMAL (18,8) NOT NULL,
    @Commission DECIMAL (18,8) NOT NULL,
    @CommissionAsset NVARCHAR(100) NOT NULL,
    @Time DATETIME2(7) NOT NULL,
    @IsBuyer BIT NOT NULL,
    @IsMaker BIT NOT NULL,
    @IsBestMatch BIT NOT NULL
AS

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
AND [S.].[Id] = [T].[Id]
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