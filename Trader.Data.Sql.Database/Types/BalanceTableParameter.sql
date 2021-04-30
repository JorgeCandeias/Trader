CREATE TYPE [dbo].[BalanceTableParameter] AS TABLE
(
	[Asset] NVARCHAR(100) NOT NULL,
	[Free] DECIMAL(18,8) NOT NULL,
	[Locked] DECIMAL(18,8) NOT NULL,
	[UpdatedTime] DATETIME2(7) NOT NULL
)
GO