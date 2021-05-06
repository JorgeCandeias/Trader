CREATE PROCEDURE [dbo].[SetPagedTrade]
	@Symbol NVARCHAR(100),
	@TradeId BIGINT
AS

SET NOCOUNT ON;

DECLARE @SymbolId INT;
EXECUTE [dbo].[GetOrAddSymbol] @Name = @Symbol, @Id = @SymbolId OUTPUT;

WITH [Source] AS
(
	SELECT
		@SymbolId AS [SymbolId],
		@TradeId AS [TradeId]
)
MERGE INTO [dbo].[PagedTrade] AS [T]
USING [Source] AS [S]
ON [S].[SymbolId] = [T].[SymbolId]
WHEN MATCHED THEN
UPDATE SET
	[TradeId] = [S].[TradeId]
WHEN NOT MATCHED BY TARGET THEN
INSERT
(
	[SymbolId],
	[TradeId]
)
VALUES
(
	[SymbolId],
	[TradeId]
);

RETURN 0
GO