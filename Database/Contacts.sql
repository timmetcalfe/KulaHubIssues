CREATE TABLE [dbo].[Contacts]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ClientId] INT NOT NULL,
    [OrganisationId] INT NULL,
    [FirstName] NVARCHAR(50) NULL,
    [LastName] NVARCHAR(50) NULL,
    [Email] NVARCHAR(60) NULL,
    [Postcode] NVARCHAR(12) NULL,
    [CreatedUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_Contacts_CreatedUtc] DEFAULT SYSUTCDATETIME(),
    [CreatedBy] NVARCHAR(100) NOT NULL,
    [ModifiedUtc] DATETIME2(7) NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [DeletedUtc] DATETIME2(7) NULL,
    CONSTRAINT [PK_Contacts] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Contacts_Clients] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Clients] ([Id]),
    CONSTRAINT [FK_Contacts_Organisations] FOREIGN KEY ([OrganisationId]) REFERENCES [dbo].[Organisations] ([Id])
)
GO

CREATE INDEX [IX_Contacts_ClientId]
    ON [dbo].[Contacts] ([ClientId]);
GO

CREATE INDEX [IX_Contacts_Email]
    ON [dbo].[Contacts] ([ClientId], [Email])
    WHERE [Email] IS NOT NULL;
GO

CREATE INDEX [IX_Contacts_OrganisationId]
    ON [dbo].[Contacts] ([OrganisationId]);