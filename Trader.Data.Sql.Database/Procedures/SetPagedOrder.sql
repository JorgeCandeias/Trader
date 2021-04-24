CREATE PROCEDURE [dbo].[SetPagedOrder]
	@Symbol NVARCHAR(100),
	@OrderId BIGINT
AS

SET NOCOUNT ON;

WITH [Source] AS
(
	SELECT
		@Symbol AS [Symbol],
		@OrderId AS [OrderId]
)
MERGE INTO [dbo].[PagedOrder] AS [T]
USING [Source] AS [S]
ON [S].[Symbol] = [T].[Symbol]
WHEN MATCHED THEN
UPDATE SET
	[OrderId] = [S].[OrderId]
WHEN NOT MATCHED BY TARGET THEN
INSERT
(
	[Symbol],
	[OrderId]
)
VALUES
(
	[Symbol],
	[OrderId]
);

RETURN 0
