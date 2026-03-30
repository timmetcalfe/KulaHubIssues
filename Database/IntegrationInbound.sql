CREATE TABLE [dbo].[IntegrationInbound]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [ClientId] INT NOT NULL,
    [OriginType] NVARCHAR(50) NOT NULL,
    [EntityType] NVARCHAR(100) NOT NULL,
    [EventType] NVARCHAR(100) NOT NULL,
    [ChangeType] NVARCHAR(50) NOT NULL,
    [ExternalEntityId] NVARCHAR(100) NULL,
    [PayloadJson] NVARCHAR(MAX) NOT NULL,
    [ReceivedUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_IntegrationInbound_ReceivedUtc] DEFAULT SYSUTCDATETIME(),
    [DispatchedUtc] DATETIME2(7) NULL,
    [DispatchTarget] NVARCHAR(200) NULL,
    [ProcessedUtc] DATETIME2(7) NULL,
    CONSTRAINT [PK_IntegrationInbound] PRIMARY KEY CLUSTERED ([Id] ASC)
    --, CONSTRAINT [FK_IntegrationInbound_Clients] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Clients] ([ClientId])
)
GO

CREATE INDEX [IX_IntegrationInbound_ClientId]
    ON [dbo].[IntegrationInbound] ([ClientId]);
GO

CREATE INDEX [IX_IntegrationInbound_Unprocessed]
    ON [dbo].[IntegrationInbound] ([ProcessedUtc], [ReceivedUtc])
    WHERE [ProcessedUtc] IS NULL;
GO

CREATE INDEX [IX_IntegrationInbound_Undispatched]
    ON [dbo].[IntegrationInbound] ([DispatchedUtc], [ReceivedUtc])
    WHERE [DispatchedUtc] IS NULL;
GO