USE [one-c-sharp];
GO

CREATE TABLE [metadata].[infobases](
	[key] [uniqueidentifier] NOT NULL,
	[version] [rowversion] NOT NULL,
	[name]     [nvarchar](100) NOT NULL,
	[server]   [nvarchar](100) NOT NULL,
	[database] [nvarchar](100) NOT NULL,
	[username] [nvarchar](100) NOT NULL,
	[password] [nvarchar](100) NOT NULL,
 CONSTRAINT [PK_infobases] PRIMARY KEY CLUSTERED 
(
	[key] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

INSERT [metadata].[infobases] ([key], [name], [server], [database], [username], [password])
VALUES (CAST(0x00000000000000000000000000000000 AS uniqueidentifier), N'Metadata', N'', N'', N'', N'');
GO

