CREATE TABLE [dbo].[Notes]
(
    [NoteId] INT IDENTITY(1,1) NOT NULL,
    [ClientId] INT NOT NULL,
    [ContactId] INT NOT NULL,
    [Content] NVARCHAR(MAX) NOT NULL,
    [CreatedUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_Notes_CreatedUtc] DEFAULT SYSUTCDATETIME(),
    [CreatedBy] NVARCHAR(100) NOT NULL,
    [ModifiedUtc] DATETIME2(7) NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [DeletedUtc] DATETIME2(7) NULL,
    CONSTRAINT [PK_Notes] PRIMARY KEY CLUSTERED ([NoteId] ASC),
    CONSTRAINT [FK_Notes_Clients] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Clients] ([ClientId]),
    CONSTRAINT [FK_Notes_Contacts] FOREIGN KEY ([ContactId]) REFERENCES [dbo].[Contacts] ([ContactId])
)
GO

CREATE INDEX [IX_Notes_ClientId]
    ON [dbo].[Notes] ([ClientId]);
GO

CREATE INDEX [IX_Notes_ContactId]
    ON [dbo].[Notes] ([ContactId]);
GO