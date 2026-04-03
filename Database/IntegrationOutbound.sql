CREATE TABLE [dbo].[IntegrationOutbound]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [CorrelationId] NVARCHAR(32) NULL,
    [TraceParent] NVARCHAR(55) NULL,
    [ClientId] INT NOT NULL,
    [OriginType] NVARCHAR(50) NOT NULL,
    [EntityType] NVARCHAR(100) NOT NULL,
    [EventType] NVARCHAR(100) NOT NULL,
    [ChangeType] NVARCHAR(50) NOT NULL,
    [ExternalEntityId] NVARCHAR(100) NULL,
    [PayloadJson] NVARCHAR(MAX) NOT NULL,
    [ReceivedUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_IntegrationOutbound_ReceivedUtc] DEFAULT SYSUTCDATETIME(),
    [DispatchedUtc] DATETIME2(7) NULL,
    [DispatchTarget] NVARCHAR(200) NULL,
    [ProcessedUtc] DATETIME2(7) NULL,
    CONSTRAINT [PK_IntegrationOutbound] PRIMARY KEY CLUSTERED ([Id] ASC)
    --, CONSTRAINT [FK_IntegrationOutbound_Clients] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Clients] ([ClientId])
)
GO

CREATE INDEX [IX_IntegrationOutbound_ClientId]
    ON [dbo].[IntegrationOutbound] ([ClientId]);
GO

CREATE INDEX [IX_IntegrationOutbound_Unprocessed]
    ON [dbo].[IntegrationOutbound] ([ProcessedUtc], [ReceivedUtc])
    WHERE [ProcessedUtc] IS NULL;
GO

CREATE INDEX [IX_IntegrationOutbound_Undispatched]
    ON [dbo].[IntegrationOutbound] ([DispatchedUtc], [ReceivedUtc])
    WHERE [DispatchedUtc] IS NULL;
GO