CREATE PROCEDURE [dbo].[SetBalances]
	@Balances [dbo].[BalanceTableParameter] READONLY
AS

SET NOCOUNT ON;

MERGE INTO [dbo].[Balance] AS [T]
USING @Balances AS [S]
ON [S].[Asset] = [T].[Asset]
WHEN NOT MATCHED BY TARGET THEN
INSERT ([Asset], [Free], [Locked], [UpdatedTime])
VALUES ([Asset], [Free], [Locked], [UpdatedTime])
WHEN MATCHED AND [S].[UpdatedTime] >= [T].[UpdatedTime] THEN
UPDATE SET
	[Free] = [S].[Free],
	[Locked] = [S].[Locked],
	[UpdatedTime] = [S].[UpdatedTime]
;

RETURN 0
GO