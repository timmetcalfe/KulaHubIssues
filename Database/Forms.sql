CREATE TABLE [dbo].[Forms]
(
    [FormId] INT IDENTITY(1,1) NOT NULL,
    [ClientId] INT NOT NULL,
    [FormTypeId] INT NOT NULL,
    [OrganisationId] INT NULL,
    [ContactId] INT NULL,
    [Text1] NVARCHAR(MAX) NULL,
    [Text2] NVARCHAR(MAX) NULL,
    [Text3] NVARCHAR(MAX) NULL,
    [DateTime1] DATETIME2(7) NULL,
    [DateTime2] DATETIME2(7) NULL,
    [CreatedUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_Forms_CreatedUtc] DEFAULT SYSUTCDATETIME(),
    [CreatedBy] NVARCHAR(100) NOT NULL,
    [ModifiedUtc] DATETIME2(7) NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [DeletedUtc] DATETIME2(7) NULL,
    [OriginalFormId] INT NULL,
    CONSTRAINT [PK_Forms] PRIMARY KEY CLUSTERED ([FormId] ASC),
    CONSTRAINT [FK_Forms_Clients] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Clients] ([ClientId]),
    CONSTRAINT [FK_Forms_FormTypes] FOREIGN KEY ([FormTypeId]) REFERENCES [dbo].[FormTypes] ([FormTypeId]),
    CONSTRAINT [FK_Forms_Organisations] FOREIGN KEY ([OrganisationId]) REFERENCES [dbo].[Organisations] ([OrganisationId]),
    CONSTRAINT [FK_Forms_Contacts] FOREIGN KEY ([ContactId]) REFERENCES [dbo].[Contacts] ([ContactId]),
    CONSTRAINT [CK_Forms_HasOwner] CHECK ([OrganisationId] IS NOT NULL OR [ContactId] IS NOT NULL)
)
GO

CREATE INDEX [IX_Forms_ClientId]
    ON [dbo].[Forms] ([ClientId]);
GO

CREATE INDEX [IX_Forms_FormTypeId]
    ON [dbo].[Forms] ([FormTypeId]);
GO

CREATE INDEX [IX_Forms_OrganisationId]
    ON [dbo].[Forms] ([OrganisationId]);
GO

CREATE INDEX [IX_Forms_ContactId]
    ON [dbo].[Forms] ([ContactId]);
GO