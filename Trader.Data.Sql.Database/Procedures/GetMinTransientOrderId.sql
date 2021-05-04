CREATE PROCEDURE [dbo].[GetMinTransientOrderId]
	@Symbol NVARCHAR(100)
AS

SET NOCOUNT ON;

SELECT
	MIN([O].[OrderId])
FROM
	[dbo].[Order] AS [O]
	INNER JOIN [dbo].[Symbol] AS [S]
		ON [S].[Id] = [O].[SymbolId]
WHERE
	[S].[Name] = @Symbol
	AND [O].[IsTransient] = 1

RETURN 0
GO