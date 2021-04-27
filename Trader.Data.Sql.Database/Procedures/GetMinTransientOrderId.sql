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
	AND [IsTransient] = 1

RETURN 0
GO