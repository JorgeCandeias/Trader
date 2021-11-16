CREATE PROCEDURE [dbo].[SetTicker]
	@Symbol NVARCHAR(100),
	@EventTime DATETIME2(7),
	@ClosePrice DECIMAL(18,8),
	@OpenPrice DECIMAL(18,8),
	@HighPrice DECIMAL(18,8),
	@LowPrice DECIMAL(18,8),
	@AssetVolume DECIMAL(18,8),
	@QuoteVolume DECIMAL(18,8)
AS

SET XACT_ABORT ON;
SET NOCOUNT ON;

DECLARE @SymbolId INT;
EXECUTE [dbo].[GetOrAddSymbol] @Name = @Symbol, @Id = @SymbolId OUT;

WITH [Source] AS
(
	SELECT
		@SymbolId AS [SymbolId],
		@EventTime AS [EventTime],
		@ClosePrice AS [ClosePrice],
		@OpenPrice AS [OpenPrice],
		@HighPrice AS [HighPrice],
		@LowPrice AS [LowPrice],
		@AssetVolume AS [AssetVolume],
		@QuoteVolume AS [QuoteVolume]
)

MERGE INTO [dbo].[Ticker] WITH (UPDLOCK, HOLDLOCK) AS [T]
USING [Source] AS [S]
ON [T].[SymbolId] = [S].[SymbolId]
WHEN NOT MATCHED BY TARGET THEN
INSERT
(
	[SymbolId],
	[EventTime],
	[ClosePrice],
	[OpenPrice],
	[HighPrice],
	[LowPrice],
	[AssetVolume],
	[QuoteVolume]
)
VALUES
(
	[SymbolId],
	[EventTime],
	[ClosePrice],
	[OpenPrice],
	[HighPrice],
	[LowPrice],
	[AssetVolume],
	[QuoteVolume]
)
WHEN MATCHED AND [S].[EventTime] >= [T].[EventTime] THEN
UPDATE SET
	[EventTime] = [S].[EventTime],
	[ClosePrice] = [S].[ClosePrice],
	[OpenPrice] = [S].[OpenPrice],
	[HighPrice] = [S].[HighPrice],
	[LowPrice] = [S].[LowPrice],
	[AssetVolume] = [S].[AssetVolume],
	[QuoteVolume] = [S].[QuoteVolume]
;

RETURN 0
GO