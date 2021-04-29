CREATE PROCEDURE [dbo].[SetPagedTrade]
	@Symbol NVARCHAR(100),
	@TradeId BIGINT
AS

SET NOCOUNT ON;

WITH [Source] AS
(
	SELECT
		@Symbol AS [Symbol],
		@TradeId AS [TradeId]
)
MERGE INTO [dbo].[PagedTrade] AS [T]
USING [Source] AS [S]
ON [S].[Symbol] = [T].[Symbol]
WHEN MATCHED THEN
UPDATE SET
	[TradeId] = [S].[TradeId]
WHEN NOT MATCHED BY TARGET THEN
INSERT
(
	[Symbol],
	[TradeId]
)
VALUES
(
	[Symbol],
	[TradeId]
);

RETURN 0
