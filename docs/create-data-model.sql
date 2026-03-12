SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE dbo.Clients (
    Id int IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Clients PRIMARY KEY,
    Name nvarchar(200) NOT NULL,
    Postcode nvarchar(12) NULL,
    CreatedUtc datetime2(7) NOT NULL
        CONSTRAINT DF_Clients_CreatedUtc DEFAULT SYSUTCDATETIME(),
    CreatedBy nvarchar(100) NOT NULL,
    ModifiedUtc datetime2(7) NULL,
    ModifiedBy nvarchar(100) NULL,
    DeletedUtc datetime2(7) NULL
);
GO

CREATE TABLE dbo.Organisations (
    Id int IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Organisations PRIMARY KEY,
    ClientId int NOT NULL,
    Name nvarchar(100) NOT NULL,
    Postcode nvarchar(12) NULL,
    CreatedUtc datetime2(7) NOT NULL
        CONSTRAINT DF_Organisations_CreatedUtc DEFAULT SYSUTCDATETIME(),
    CreatedBy nvarchar(100) NOT NULL,
    ModifiedUtc datetime2(7) NULL,
    ModifiedBy nvarchar(100) NULL,
    DeletedUtc datetime2(7) NULL,
    CONSTRAINT FK_Organisations_Clients
        FOREIGN KEY (ClientId) REFERENCES dbo.Clients (Id)
);
GO

CREATE TABLE dbo.Contacts (
    Id int IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Contacts PRIMARY KEY,
    ClientId int NOT NULL,
    OrganisationId int NULL,
    FirstName nvarchar(50) NULL,
    LastName nvarchar(50) NULL,
    Email nvarchar(60) NULL,
    Postcode nvarchar(12) NULL,
    CreatedUtc datetime2(7) NOT NULL
        CONSTRAINT DF_Contacts_CreatedUtc DEFAULT SYSUTCDATETIME(),
    CreatedBy nvarchar(100) NOT NULL,
    ModifiedUtc datetime2(7) NULL,
    ModifiedBy nvarchar(100) NULL,
    DeletedUtc datetime2(7) NULL,
    CONSTRAINT FK_Contacts_Clients
        FOREIGN KEY (ClientId) REFERENCES dbo.Clients (Id),
    CONSTRAINT FK_Contacts_Organisations
        FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (Id)
);
GO

CREATE TABLE dbo.FormTypes (
    Id int IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_FormTypes PRIMARY KEY,
    ClientId int NOT NULL,
    Name nvarchar(max) NULL,
    CreatedUtc datetime2(7) NOT NULL
        CONSTRAINT DF_FormTypes_CreatedUtc DEFAULT SYSUTCDATETIME(),
    CreatedBy nvarchar(100) NOT NULL,
    ModifiedUtc datetime2(7) NULL,
    ModifiedBy nvarchar(100) NULL,
    DeletedUtc datetime2(7) NULL,
    CONSTRAINT FK_FormTypes_Clients
        FOREIGN KEY (ClientId) REFERENCES dbo.Clients (Id)
);
GO

CREATE TABLE dbo.Forms (
    Id int IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Forms PRIMARY KEY,
    ClientId int NOT NULL,
    FormTypeId int NOT NULL,
    OrganisationId int NULL,
    ContactId int NULL,
    Text1 nvarchar(max) NULL,
    Text2 nvarchar(max) NULL,
    DateTime1 datetime2(7) NULL,
    DateTime2 datetime2(7) NULL,
    CreatedUtc datetime2(7) NOT NULL
        CONSTRAINT DF_Forms_CreatedUtc DEFAULT SYSUTCDATETIME(),
    CreatedBy nvarchar(100) NOT NULL,
    ModifiedUtc datetime2(7) NULL,
    ModifiedBy nvarchar(100) NULL,
    DeletedUtc datetime2(7) NULL,
    CONSTRAINT FK_Forms_Clients
        FOREIGN KEY (ClientId) REFERENCES dbo.Clients (Id),
    CONSTRAINT FK_Forms_FormTypes
        FOREIGN KEY (FormTypeId) REFERENCES dbo.FormTypes (Id),
    CONSTRAINT FK_Forms_Organisations
        FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (Id),
    CONSTRAINT FK_Forms_Contacts
        FOREIGN KEY (ContactId) REFERENCES dbo.Contacts (Id),
    CONSTRAINT CK_Forms_HasOwner
        CHECK (OrganisationId IS NOT NULL OR ContactId IS NOT NULL)
);
GO

CREATE INDEX IX_Organisations_ClientId
    ON dbo.Organisations (ClientId);
GO

CREATE INDEX IX_Contacts_ClientId
    ON dbo.Contacts (ClientId);
GO

CREATE INDEX IX_Contacts_Email
    ON dbo.Contacts (ClientId, Email)
    WHERE Email IS NOT NULL;
GO

CREATE INDEX IX_Contacts_OrganisationId
    ON dbo.Contacts (OrganisationId);
GO

CREATE INDEX IX_FormTypes_ClientId
    ON dbo.FormTypes (ClientId);
GO

CREATE INDEX IX_Forms_ClientId
    ON dbo.Forms (ClientId);
GO

CREATE INDEX IX_Forms_FormTypeId
    ON dbo.Forms (FormTypeId);
GO

CREATE INDEX IX_Forms_OrganisationId
    ON dbo.Forms (OrganisationId);
GO

CREATE INDEX IX_Forms_ContactId
    ON dbo.Forms (ContactId);
GO

COMMIT TRANSACTION;
GO