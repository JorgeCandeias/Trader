CREATE PROCEDURE [dbo].[GetMinTransientOrderId]
	@Symbol NVARCHAR(100)
AS

SET NOCOUNT ON;

SELECT
	MIN([OrderId])
FROM
	[dbo].[Order]
WHERE
	[Symbol] = @Symbol
	AND [Status] IN
	(
		1 /* New */,
		2 /* PartiallyFilled */,
		5 /* PendingCancel */
	)

RETURN 0
GO