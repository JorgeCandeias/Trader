CREATE PROCEDURE [dbo].[GetPagedOrder]
	@Symbol NVARCHAR(100)
AS

SET NOCOUNT ON;

SELECT
	[OrderId]
FROM
	[dbo].[PagedOrder]
WHERE
	[Symbol] = @Symbol

RETURN 0
