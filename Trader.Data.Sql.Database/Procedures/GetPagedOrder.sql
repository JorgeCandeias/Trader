CREATE PROCEDURE [dbo].[GetPagedOrder]
	@Symbol NVARCHAR(100)
AS

SET NOCOUNT ON;

SELECT
	[OrderId]
FROM
	[dbo].[PagedOrder] AS [PO]
	INNER JOIN [dbo].[Symbol] AS [S]
		ON [S].[Id] = [PO].[SymbolId]
WHERE
	[S].[Name] = @Symbol

RETURN 0
GO