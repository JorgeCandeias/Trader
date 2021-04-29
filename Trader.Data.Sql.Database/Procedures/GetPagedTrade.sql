CREATE PROCEDURE [dbo].[GetPagedTrade]
	@Symbol NVARCHAR(100)
AS

SET NOCOUNT ON;

SELECT
	[TradeId]
FROM
	[dbo].[PagedTrade]
WHERE
	[Symbol] = @Symbol

RETURN 0
