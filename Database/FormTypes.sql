CREATE TABLE [dbo].[FormTypes]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ClientId] INT NOT NULL,
    [Name] NVARCHAR(MAX) NULL,
    [CreatedUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_FormTypes_CreatedUtc] DEFAULT SYSUTCDATETIME(),
    [CreatedBy] NVARCHAR(100) NOT NULL,
    [ModifiedUtc] DATETIME2(7) NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [DeletedUtc] DATETIME2(7) NULL,
    CONSTRAINT [PK_FormTypes] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_FormTypes_Clients] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Clients] ([Id])
)
GO

CREATE INDEX [IX_FormTypes_ClientId]
    ON [dbo].[FormTypes] ([ClientId]);