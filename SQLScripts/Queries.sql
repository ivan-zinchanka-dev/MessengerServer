USE chat_app
GO

CREATE TABLE [User] (
	[Nickname] NVARCHAR(30) NOT NULL,
	[Password] NVARCHAR(30) NOT NULL,

	PRIMARY KEY ([Nickname])
)

GO

CREATE TABLE [Message](
	[SenderNickname] NVARCHAR(30) NOT NULL,
	[ReceiverNickname] NVARCHAR(30) NULL,
	[Text] NVARCHAR(1000) NOT NULL,
	[PostDateTime] DATETIME NOT NULL,

	FOREIGN KEY ([SenderNickname]) REFERENCES [User]([Nickname]),
	FOREIGN KEY ([ReceiverNickname]) REFERENCES [User]([Nickname])
)

GO


DELETE FROM [Message]
GO 

DELETE FROM [User]
GO

DROP TABLE [Message]
GO

DROP TABLE [User]
GO

