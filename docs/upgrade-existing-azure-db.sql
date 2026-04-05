SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('dbo.IntegrationOutbound', 'U') IS NOT NULL
       AND EXISTS (SELECT 1 FROM dbo.IntegrationOutbound)
    BEGIN
        THROW 51000, 'Direct cutover blocked: dbo.IntegrationOutbound still contains rows. Drain or archive legacy outbound rows before running this migration.', 1;
    END;

    IF OBJECT_ID('dbo.IntegrationInbound', 'U') IS NOT NULL
       AND EXISTS (SELECT 1 FROM dbo.IntegrationInbound)
    BEGIN
        THROW 51001, 'Direct cutover blocked: dbo.IntegrationInbound still contains rows. Drain or archive legacy inbound rows before running this migration.', 1;
    END;

    IF COL_LENGTH('dbo.IntegrationInbox', 'CorrelationId') IS NULL
    BEGIN
        ALTER TABLE dbo.IntegrationInbox
        ADD CorrelationId NVARCHAR(32) NULL;
    END;

    IF COL_LENGTH('dbo.IntegrationInbox', 'TraceParent') IS NULL
    BEGIN
        ALTER TABLE dbo.IntegrationInbox
        ADD TraceParent NVARCHAR(55) NULL;
    END;

    IF COL_LENGTH('dbo.IntegrationInbox', 'OriginType') IS NULL
    BEGIN
        ALTER TABLE dbo.IntegrationInbox
        ADD OriginType NVARCHAR(50) NULL;
    END;

    IF COL_LENGTH('dbo.IntegrationInbox', 'SourceSystemKey') IS NULL
    BEGIN
        ALTER TABLE dbo.IntegrationInbox
        ADD SourceSystemKey NVARCHAR(100) NULL;
    END;

    UPDATE dbo.IntegrationInbox
    SET OriginType = 'ExternalClient'
    WHERE OriginType IS NULL;

    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.IntegrationInbox')
          AND name = 'OriginType'
          AND is_nullable = 1)
    BEGIN
        ALTER TABLE dbo.IntegrationInbox
        ALTER COLUMN OriginType NVARCHAR(50) NOT NULL;
    END;

    IF OBJECT_ID('dbo.IntegrationDispatch', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.IntegrationDispatch
        (
            Id BIGINT IDENTITY(1,1) NOT NULL,
            IntegrationInboxId BIGINT NOT NULL,
            CorrelationId NVARCHAR(32) NULL,
            TraceParent NVARCHAR(55) NULL,
            ClientId INT NOT NULL,
            Disposition NVARCHAR(50) NOT NULL,
            OriginType NVARCHAR(50) NOT NULL,
            SourceSystemKey NVARCHAR(100) NULL,
            QueueKey NVARCHAR(100) NOT NULL,
            EntityType NVARCHAR(100) NOT NULL,
            EventType NVARCHAR(100) NOT NULL,
            ChangeType NVARCHAR(50) NOT NULL,
            ExternalEntityId NVARCHAR(100) NULL,
            PayloadJson NVARCHAR(MAX) NOT NULL,
            ReceivedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_IntegrationDispatch_ReceivedUtc DEFAULT SYSUTCDATETIME(),
            DispatchedUtc DATETIME2(7) NULL,
            DispatchTarget NVARCHAR(200) NULL,
            ProcessedUtc DATETIME2(7) NULL,
            CONSTRAINT PK_IntegrationDispatch PRIMARY KEY CLUSTERED (Id ASC)
        );
    END;

    IF COL_LENGTH('dbo.IntegrationDispatch', 'IntegrationInboxId') IS NULL
    BEGIN
        THROW 51002, 'dbo.IntegrationDispatch exists but does not match the expected schema. Apply the schema manually or recreate the table before rerunning this migration.', 1;
    END;

    IF COL_LENGTH('dbo.IntegrationDispatch', 'CorrelationId') IS NULL
    BEGIN
        ALTER TABLE dbo.IntegrationDispatch ADD CorrelationId NVARCHAR(32) NULL;
    END;

    IF COL_LENGTH('dbo.IntegrationDispatch', 'TraceParent') IS NULL
    BEGIN
        ALTER TABLE dbo.IntegrationDispatch ADD TraceParent NVARCHAR(55) NULL;
    END;

    IF COL_LENGTH('dbo.IntegrationDispatch', 'Disposition') IS NULL
    BEGIN
        ALTER TABLE dbo.IntegrationDispatch ADD Disposition NVARCHAR(50) NULL;
    END;

    IF COL_LENGTH('dbo.IntegrationDispatch', 'OriginType') IS NULL
    BEGIN
        ALTER TABLE dbo.IntegrationDispatch ADD OriginType NVARCHAR(50) NULL;
    END;

    IF COL_LENGTH('dbo.IntegrationDispatch', 'SourceSystemKey') IS NULL
    BEGIN
        ALTER TABLE dbo.IntegrationDispatch ADD SourceSystemKey NVARCHAR(100) NULL;
    END;

    IF COL_LENGTH('dbo.IntegrationDispatch', 'QueueKey') IS NULL
    BEGIN
        ALTER TABLE dbo.IntegrationDispatch ADD QueueKey NVARCHAR(100) NULL;
    END;

    IF COL_LENGTH('dbo.IntegrationDispatch', 'DispatchedUtc') IS NULL
    BEGIN
        ALTER TABLE dbo.IntegrationDispatch ADD DispatchedUtc DATETIME2(7) NULL;
    END;

    IF COL_LENGTH('dbo.IntegrationDispatch', 'DispatchTarget') IS NULL
    BEGIN
        ALTER TABLE dbo.IntegrationDispatch ADD DispatchTarget NVARCHAR(200) NULL;
    END;

    IF COL_LENGTH('dbo.IntegrationDispatch', 'ProcessedUtc') IS NULL
    BEGIN
        ALTER TABLE dbo.IntegrationDispatch ADD ProcessedUtc DATETIME2(7) NULL;
    END;

    IF COL_LENGTH('dbo.IntegrationDispatch', 'ReceivedUtc') IS NULL
    BEGIN
        ALTER TABLE dbo.IntegrationDispatch
        ADD ReceivedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_IntegrationDispatch_ReceivedUtc DEFAULT SYSUTCDATETIME();
    END;

    EXEC sys.sp_executesql N'
        UPDATE dbo.IntegrationDispatch
        SET OriginType = ISNULL(OriginType, ''ExternalClient''),
            Disposition = ISNULL(Disposition, ''Outbound''),
            QueueKey = ISNULL(QueueKey, ''Unknown'')
        WHERE OriginType IS NULL
           OR Disposition IS NULL
           OR QueueKey IS NULL;';

    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.IntegrationDispatch')
          AND name = 'Disposition'
          AND is_nullable = 1)
    BEGIN
        EXEC sys.sp_executesql N'
            ALTER TABLE dbo.IntegrationDispatch
            ALTER COLUMN Disposition NVARCHAR(50) NOT NULL;';
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.IntegrationDispatch')
          AND name = 'OriginType'
          AND is_nullable = 1)
    BEGIN
        EXEC sys.sp_executesql N'
            ALTER TABLE dbo.IntegrationDispatch
            ALTER COLUMN OriginType NVARCHAR(50) NOT NULL;';
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.IntegrationDispatch')
          AND name = 'QueueKey'
          AND is_nullable = 1)
    BEGIN
        EXEC sys.sp_executesql N'
            ALTER TABLE dbo.IntegrationDispatch
            ALTER COLUMN QueueKey NVARCHAR(100) NOT NULL;';
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_IntegrationInbox_SourceSystemKey'
          AND object_id = OBJECT_ID('dbo.IntegrationInbox'))
    BEGIN
        EXEC sys.sp_executesql N'
            CREATE INDEX IX_IntegrationInbox_SourceSystemKey
                ON dbo.IntegrationInbox (SourceSystemKey)
                WHERE SourceSystemKey IS NOT NULL;';
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_IntegrationDispatch_ClientId'
          AND object_id = OBJECT_ID('dbo.IntegrationDispatch'))
    BEGIN
        CREATE INDEX IX_IntegrationDispatch_ClientId
            ON dbo.IntegrationDispatch (ClientId);
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_IntegrationDispatch_InboxId'
          AND object_id = OBJECT_ID('dbo.IntegrationDispatch'))
    BEGIN
        EXEC sys.sp_executesql N'
            CREATE INDEX IX_IntegrationDispatch_InboxId
                ON dbo.IntegrationDispatch (IntegrationInboxId);';
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_IntegrationDispatch_QueueKey'
          AND object_id = OBJECT_ID('dbo.IntegrationDispatch'))
    BEGIN
        EXEC sys.sp_executesql N'
            CREATE INDEX IX_IntegrationDispatch_QueueKey
                ON dbo.IntegrationDispatch (QueueKey);';
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_IntegrationDispatch_SourceSystemKey'
          AND object_id = OBJECT_ID('dbo.IntegrationDispatch'))
    BEGIN
        EXEC sys.sp_executesql N'
            CREATE INDEX IX_IntegrationDispatch_SourceSystemKey
                ON dbo.IntegrationDispatch (SourceSystemKey)
                WHERE SourceSystemKey IS NOT NULL;';
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_IntegrationDispatch_Unprocessed'
          AND object_id = OBJECT_ID('dbo.IntegrationDispatch'))
    BEGIN
        EXEC sys.sp_executesql N'
            CREATE INDEX IX_IntegrationDispatch_Unprocessed
                ON dbo.IntegrationDispatch (ProcessedUtc, ReceivedUtc)
                WHERE ProcessedUtc IS NULL;';
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_IntegrationDispatch_Undispatched'
          AND object_id = OBJECT_ID('dbo.IntegrationDispatch'))
    BEGIN
        EXEC sys.sp_executesql N'
            CREATE INDEX IX_IntegrationDispatch_Undispatched
                ON dbo.IntegrationDispatch (DispatchedUtc, ReceivedUtc)
                WHERE DispatchedUtc IS NULL;';
    END;

    IF OBJECT_ID('dbo.IntegrationOutbound', 'U') IS NOT NULL
    BEGIN
        DROP TABLE dbo.IntegrationOutbound;
    END;

    IF OBJECT_ID('dbo.IntegrationInbound', 'U') IS NOT NULL
    BEGIN
        DROP TABLE dbo.IntegrationInbound;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;
GO
