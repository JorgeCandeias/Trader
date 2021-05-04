CREATE TABLE [dbo].[Symbol]
(
	[Id] INT NOT NULL DEFAULT NEXT VALUE FOR [dbo].[SymbolSequence],
	[Name] NVARCHAR(100) NOT NULL,

	CONSTRAINT [PK_Symbol] PRIMARY KEY CLUSTERED
	(
		[Id]
	),

	CONSTRAINT [UK_Symbol_Name] UNIQUE
	(
		[Name]
	)
)
GO