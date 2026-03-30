CREATE TABLE [dbo].[IntegrationInbox]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [ClientId] INT NOT NULL,
    [OriginType] NVARCHAR(50) NOT NULL,
    [EntityType] NVARCHAR(100) NOT NULL,
    [EventType] NVARCHAR(100) NOT NULL,
    [ChangeType] NVARCHAR(50) NOT NULL,
    [ExternalEntityId] NVARCHAR(100) NULL,
    [PayloadJson] NVARCHAR(MAX) NOT NULL,
    [ReceivedUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_IntegrationInbox_ReceivedUtc] DEFAULT SYSUTCDATETIME(),
    [ProcessedUtc] DATETIME2(7) NULL,
    CONSTRAINT [PK_IntegrationInbox] PRIMARY KEY CLUSTERED ([Id] ASC)
    --, CONSTRAINT [FK_IntegrationInbox_Clients] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Clients] ([ClientId])
)
GO

CREATE INDEX [IX_IntegrationInbox_ClientId]
    ON [dbo].[IntegrationInbox] ([ClientId]);
GO

CREATE INDEX [IX_IntegrationInbox_Unprocessed]
    ON [dbo].[IntegrationInbox] ([ProcessedUtc], [ReceivedUtc])
    WHERE [ProcessedUtc] IS NULL;
GO