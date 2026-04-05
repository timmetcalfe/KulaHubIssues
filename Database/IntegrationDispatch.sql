CREATE TABLE [dbo].[IntegrationDispatch]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [IntegrationInboxId] BIGINT NOT NULL,
    [CorrelationId] NVARCHAR(32) NULL,
    [TraceParent] NVARCHAR(55) NULL,
    [ClientId] INT NOT NULL,
    [Disposition] NVARCHAR(50) NOT NULL,
    [OriginType] NVARCHAR(50) NOT NULL,
    [SourceSystemKey] NVARCHAR(100) NULL,
    [QueueKey] NVARCHAR(100) NOT NULL,
    [EntityType] NVARCHAR(100) NOT NULL,
    [EventType] NVARCHAR(100) NOT NULL,
    [ChangeType] NVARCHAR(50) NOT NULL,
    [ExternalEntityId] NVARCHAR(100) NULL,
    [PayloadJson] NVARCHAR(MAX) NOT NULL,
    [ReceivedUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_IntegrationDispatch_ReceivedUtc] DEFAULT SYSUTCDATETIME(),
    [DispatchedUtc] DATETIME2(7) NULL,
    [DispatchTarget] NVARCHAR(200) NULL,
    [ProcessedUtc] DATETIME2(7) NULL,
    CONSTRAINT [PK_IntegrationDispatch] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO

CREATE INDEX [IX_IntegrationDispatch_ClientId]
    ON [dbo].[IntegrationDispatch] ([ClientId]);
GO

CREATE INDEX [IX_IntegrationDispatch_InboxId]
    ON [dbo].[IntegrationDispatch] ([IntegrationInboxId]);
GO

CREATE INDEX [IX_IntegrationDispatch_QueueKey]
    ON [dbo].[IntegrationDispatch] ([QueueKey]);
GO

CREATE INDEX [IX_IntegrationDispatch_SourceSystemKey]
    ON [dbo].[IntegrationDispatch] ([SourceSystemKey])
    WHERE [SourceSystemKey] IS NOT NULL;
GO

CREATE INDEX [IX_IntegrationDispatch_Unprocessed]
    ON [dbo].[IntegrationDispatch] ([ProcessedUtc], [ReceivedUtc])
    WHERE [ProcessedUtc] IS NULL;
GO

CREATE INDEX [IX_IntegrationDispatch_Undispatched]
    ON [dbo].[IntegrationDispatch] ([DispatchedUtc], [ReceivedUtc])
    WHERE [DispatchedUtc] IS NULL;
GO