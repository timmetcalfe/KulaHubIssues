# Data Model
This document describes the data models used in KulaHub.

## Overview
This system supports clients(tenants), organisations, contacts and forms for a multi-tenant SaaS CRM application.

## Conventions and assumptions
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
| FormTypeId | uniqueidentifier | Yes | FK → FormTypes.Id | | The FormType definition |
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
| Forms.ClientId | Clients.Id | Many-to-One | A form belongs to one client |
| Forms.FormTypeId | FormTypes.Id | Many-to-One | A form optionally references one form type definition |
| Forms.OrganisationId | Organisations.Id | Many-to-One | A form optionally belongs to one organisation |
| Forms.ContactId | Contacts.Id | Many-to-One | A form optionally belongs to one contact |

