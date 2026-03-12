# Data Model
This document describes the data models used in KulaHub.

## Overview
This system supports clients(tenants), organisations, contacts and forms for a multi-tenant SaaS CRM application.

## Conventions and assumptions
- The database will be Azure SQL Server
- All tables use `Id` as PK unless otherwise stated
- All client-owned tables include `ClientId`
- Soft delete uses `DeletedUtc`
- Audit fields: `CreatedUtc`, `CreatedBy`, `ModifiedUtc`, `ModifiedBy`. Automatically include these when creating new tables.

## Entities / Tables

### Clients
Represents the organisations that are using the system. Each client will have many organisations, contacts and forms

| Column | Type | Nullable | Key | Default | Notes |
|------|------|------|------|------|------|
| Id | uniqueidentifier | No | PK | | Primary key |
| Name | nvarchar(200) | No | | | Display name |
| Postcode | nvarchar(12) | Yes | | | |

### Organisations
Represent the organisations that a client is dealing with.

| Column | Type | Nullable | Key | Default | Notes |
|------|------|------|------|------|------|
| Id | uniqueidentifier | No | PK | | Primary key |
| ClientId | uniqueidentifier | No | FK → Clients.Id | | Client owner |
| Name | nvarchar(100) | No | | | Display name |
| Postcode | nvarchar(12) | Yes | | | |

### Contacts
Represents the contacts of the CRM system
| Column | Type | Nullable | Key | Default | Notes |
|------|------|------|------|------|------|
| Id | uniqueidentifier | No | PK | | Primary key |
| ClientId | uniqueidentifier | No | FK → Clients.Id | | Client owner |
| OrganisationId | uniqueidentifier | Yes | FK → Organisations.Id | | Organisation owner |
| FirstName | nvarchar(50) | Yes | | | |
| LastName | nvarchar(50) | Yes | | | |
| Email | nvarchar(60) | Yes | | | |
| Postcode | nvarchar(12) | Yes | | | |

### FormTypes
Represents the definition form forms
| Column | Type | Nullable | Key | Default | Notes |
|------|------|------|------|------|------|
| Id | uniqueidentifier | No | PK | | Primary key |
| ClientId | uniqueidentifier | No | FK → Clients.Id | | Client owner |
| Name | nvarchar(max) | Yes | | | |

### Forms
Represents forms that can be added to a contact or organisation to store custom information
| Column | Type | Nullable | Key | Default | Notes |
|------|------|------|------|------|------|
| Id | uniqueidentifier | No | PK | | Primary key |
| ClientId | uniqueidentifier | No | FK → Clients.Id | | Client owner |
| FormTypeId | uniqueidentifier | No | FK → FormTypes.Id | | The FormType definition |
| OrganisationId | uniqueidentifier | Yes | FK → Organisations.Id | | Organisation owner |
| ContactId | uniqueidentifier | Yes | FK → Contacts.Id | | Contact owner |
| Text1 | nvarchar(max) | Yes | | | |
| Text2 | nvarchar(max) | Yes | | | |
| DateTime1 | datetime2 | Yes | | | |
| DateTime2 | datetime2 | Yes | | | |

## Relationships
| From | To | Type | Notes |
|---|---|---|---|
| Organisations.ClientId | Clients.Id | Many-to-One | An organisation belongs to one client |
| Contacts.ClientId | Clients.Id | Many-to-One | A contact belongs to one client |
| Contacts.OrganisationId | Organisations.Id | Many-to-One | A contact optionally belongs to one organisation |
| FormTypes.ClientId | Clients.Id | Many-to-One | A form type belongs to one client |
| Forms.ClientId | Clients.Id | Many-to-One | A form belongs to one client. If the form is deleted, do not cascade the delete |
| Forms.FormTypeId | FormTypes.Id | Many-to-One | A form references one form type definition. If the form is deleted, do not cascade the delete |
| Forms.OrganisationId | Organisations.Id | Many-to-One | A form optionally belongs to one organisation. If form deleted, do not cascade the delete |
| Forms.ContactId | Contacts.Id | Many-to-One | A form optionally belongs to one contact. If the form is deleted, do not cascade the delete |

## Indexes

| Index Name | Table | Columns | Notes |
|---|---|---|---|
| IX_Organisations_ClientId | Organisations | ClientId | Filter organisations by client |
| IX_Contacts_ClientId | Contacts | ClientId | Filter contacts by client |
| IX_Contacts_Email | Contacts | ClientId, Email | Look up contacts by email address within a client; filtered where Email IS NOT NULL |
| IX_Contacts_OrganisationId | Contacts | OrganisationId | Filter contacts by organisation |
| IX_FormTypes_ClientId | FormTypes | ClientId | Filter form types by client |
| IX_Forms_ClientId | Forms | ClientId | Filter forms by client |
| IX_Forms_FormTypeId | Forms | FormTypeId | Filter forms by form type |
| IX_Forms_OrganisationId | Forms | OrganisationId | Filter forms by organisation |
| IX_Forms_ContactId | Forms | ContactId | Filter forms by contact |
