SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

SET XACT_ABORT ON;
GO

IF COL_LENGTH('dbo.IntegrationOutbound', 'DispatchedUtc') IS NULL
BEGIN
    ALTER TABLE dbo.IntegrationOutbound
    ADD DispatchedUtc DATETIME2(7) NULL;
END;
GO

IF COL_LENGTH('dbo.IntegrationOutbound', 'OriginType') IS NULL
BEGIN
    ALTER TABLE dbo.IntegrationOutbound
    ADD OriginType NVARCHAR(50) NULL;
END;
GO

UPDATE dbo.IntegrationOutbound
SET OriginType = 'ExternalClient'
WHERE OriginType IS NULL;
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.IntegrationOutbound')
      AND name = 'OriginType'
      AND is_nullable = 1)
BEGIN
    ALTER TABLE dbo.IntegrationOutbound
    ALTER COLUMN OriginType NVARCHAR(50) NOT NULL;
END;
GO

IF COL_LENGTH('dbo.IntegrationOutbound', 'DispatchTarget') IS NULL
BEGIN
    ALTER TABLE dbo.IntegrationOutbound
    ADD DispatchTarget NVARCHAR(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_IntegrationOutbound_Undispatched'
      AND object_id = OBJECT_ID('dbo.IntegrationOutbound'))
BEGIN
    CREATE INDEX IX_IntegrationOutbound_Undispatched
        ON dbo.IntegrationOutbound (DispatchedUtc, ReceivedUtc)
        WHERE DispatchedUtc IS NULL;
END;
GO

IF COL_LENGTH('dbo.IntegrationInbound', 'DispatchedUtc') IS NULL
BEGIN
    ALTER TABLE dbo.IntegrationInbound
    ADD DispatchedUtc DATETIME2(7) NULL;
END;
GO

IF COL_LENGTH('dbo.IntegrationInbound', 'OriginType') IS NULL
BEGIN
    ALTER TABLE dbo.IntegrationInbound
    ADD OriginType NVARCHAR(50) NULL;
END;
GO

UPDATE dbo.IntegrationInbound
SET OriginType = 'ExternalClient'
WHERE OriginType IS NULL;
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.IntegrationInbound')
      AND name = 'OriginType'
      AND is_nullable = 1)
BEGIN
    ALTER TABLE dbo.IntegrationInbound
    ALTER COLUMN OriginType NVARCHAR(50) NOT NULL;
END;
GO

IF COL_LENGTH('dbo.IntegrationInbound', 'DispatchTarget') IS NULL
BEGIN
    ALTER TABLE dbo.IntegrationInbound
    ADD DispatchTarget NVARCHAR(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_IntegrationInbound_Undispatched'
      AND object_id = OBJECT_ID('dbo.IntegrationInbound'))
BEGIN
    CREATE INDEX IX_IntegrationInbound_Undispatched
        ON dbo.IntegrationInbound (DispatchedUtc, ReceivedUtc)
        WHERE DispatchedUtc IS NULL;
END;
GO

IF OBJECT_ID('dbo.Notes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notes
    (
        NoteId INT IDENTITY(1,1) NOT NULL,
        ClientId INT NOT NULL,
        ContactId INT NOT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_Notes_CreatedUtc DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(100) NOT NULL,
        ModifiedUtc DATETIME2(7) NULL,
        ModifiedBy NVARCHAR(100) NULL,
        DeletedUtc DATETIME2(7) NULL,
        CONSTRAINT PK_Notes PRIMARY KEY CLUSTERED (NoteId ASC),
        CONSTRAINT FK_Notes_Clients FOREIGN KEY (ClientId) REFERENCES dbo.Clients (ClientId),
        CONSTRAINT FK_Notes_Contacts FOREIGN KEY (ContactId) REFERENCES dbo.Contacts (ContactId)
    );
END;
GO

IF COL_LENGTH('dbo.IntegrationInbox', 'OriginType') IS NULL
BEGIN
    ALTER TABLE dbo.IntegrationInbox
    ADD OriginType NVARCHAR(50) NULL;
END;
GO

UPDATE dbo.IntegrationInbox
SET OriginType = 'ExternalClient'
WHERE OriginType IS NULL;
GO

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
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Notes_ClientId'
      AND object_id = OBJECT_ID('dbo.Notes'))
BEGIN
    CREATE INDEX IX_Notes_ClientId
        ON dbo.Notes (ClientId);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Notes_ContactId'
      AND object_id = OBJECT_ID('dbo.Notes'))
BEGIN
    CREATE INDEX IX_Notes_ContactId
        ON dbo.Notes (ContactId);
END;
GO
