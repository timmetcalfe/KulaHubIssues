CREATE TABLE [dbo].[Organisations]
(
    [OrganisationId] INT IDENTITY(1,1) NOT NULL,
    [ClientId] INT NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [Postcode] NVARCHAR(12) NULL,
    [CreatedUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_Organisations_CreatedUtc] DEFAULT SYSUTCDATETIME(),
    [CreatedBy] NVARCHAR(100) NOT NULL,
    [ModifiedUtc] DATETIME2(7) NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [DeletedUtc] DATETIME2(7) NULL,
    CONSTRAINT [PK_Organisations] PRIMARY KEY CLUSTERED ([OrganisationId] ASC),
    CONSTRAINT [FK_Organisations_Clients] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Clients] ([ClientId])
)
GO

CREATE INDEX [IX_Organisations_ClientId]
    ON [dbo].[Organisations] ([ClientId]);