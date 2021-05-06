CREATE PROCEDURE [dbo].[SetPagedOrder]
	@Symbol NVARCHAR(100),
	@OrderId BIGINT
AS

SET NOCOUNT ON;

DECLARE @SymbolId INT;
EXECUTE [dbo].[GetOrAddSymbol] @Name = @Symbol, @Id = @SymbolId OUTPUT;

WITH [Source] AS
(
	SELECT
		@SymbolId AS [SymbolId],
		@OrderId AS [OrderId]
)
MERGE INTO [dbo].[PagedOrder] AS [T]
USING [Source] AS [S]
ON [S].[SymbolId] = [T].[SymbolId]
WHEN MATCHED THEN
UPDATE SET
	[OrderId] = [S].[OrderId]
WHEN NOT MATCHED BY TARGET THEN
INSERT
(
	[SymbolId],
	[OrderId]
)
VALUES
(
	[SymbolId],
	[OrderId]
);

RETURN 0
GO