CREATE PROCEDURE [dbo].[GetBalances]
AS

SET NOCOUNT ON;

SELECT
	[Asset],
	[Free],
	[Locked],
	[UpdatedTime]
FROM
	[dbo].[Balance]

RETURN 0
GO