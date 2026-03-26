# Data Model
This document describes the data models used in KulaHub.

## Overview
This system supports clients(tenants), organisations, contacts, forms, and form mirror rules for a multi-tenant SaaS CRM application.

## Conventions and assumptions
- The database will be Azure SQL Server
- All tables use `EntityId` as an `int IDENTITY(1,1)` PK unless otherwise stated, where Entity is the singular table name
- All client-owned tables include `ClientId`
- Soft delete uses `DeletedUtc`
- Audit fields: `CreatedUtc`, `CreatedBy`, `ModifiedUtc`, `ModifiedBy`. Automatically include these when creating new tables, except for IntegrationInbox table.

## Entities / Tables

### Clients
Represents the organisations that are using the system. Each client will have many organisations, contacts and forms

| Column | Type | Nullable | Key | Default | Notes |
|------|------|------|------|------|------|
| ClientId | int | No | PK | IDENTITY(1,1) | Primary key |
| Name | nvarchar(200) | No | | | Display name |
| Postcode | nvarchar(12) | Yes | | | |

### Organisations
Represent the organisations that a client is dealing with.

| Column | Type | Nullable | Key | Default | Notes |
|------|------|------|------|------|------|
| OrganisationId | int | No | PK | IDENTITY(1,1) | Primary key |
| ClientId | int | No | FK → Clients.ClientId | | Client owner |
| Name | nvarchar(100) | No | | | Display name |
| Postcode | nvarchar(12) | Yes | | | |

### Contacts
Represents the contacts of the CRM system
| Column | Type | Nullable | Key | Default | Notes |
|------|------|------|------|------|------|
| ContactId | int | No | PK | IDENTITY(1,1) | Primary key |
| ClientId | int | No | FK → Clients.ClientId | | Client owner |
| OrganisationId | int | Yes | FK → Organisations.OrganisationId | | Organisation owner |
| FirstName | nvarchar(50) | Yes | | | |
| LastName | nvarchar(50) | Yes | | | |
| Email | nvarchar(60) | Yes | | | |
| Postcode | nvarchar(12) | Yes | | | |

### FormTypes
Represents the definition form forms
| Column | Type | Nullable | Key | Default | Notes |
|------|------|------|------|------|------|
| FormTypeId | int | No | PK | IDENTITY(1,1) | Primary key |
| ClientId | int | No | FK → Clients.ClientId | | Client owner |
| Name | nvarchar(max) | Yes | | | |

### Forms
Represents forms that can be added to a contact or organisation to store custom information
| Column | Type | Nullable | Key | Default | Notes |
|------|------|------|------|------|------|
| FormId | int | No | PK | IDENTITY(1,1) | Primary key |
| ClientId | int | No | FK → Clients.ClientId | | Client owner |
| FormTypeId | int | No | FK → FormTypes.FormTypeId | | The FormType definition |
| OrganisationId | int | Yes | FK → Organisations.OrganisationId | | Organisation owner |
| ContactId | int | Yes | FK → Contacts.ContactId | | Contact owner |
| Text1 | nvarchar(max) | Yes | | | |
| Text2 | nvarchar(max) | Yes | | | |
| DateTime1 | datetime2 | Yes | | | |
| DateTime2 | datetime2 | Yes | | | |

### FormMirrorRules
Represents rules for mirroring a form from one client/form type combination to another.
| Column | Type | Nullable | Key | Default | Notes |
|------|------|------|------|------|------|
| FormMirrorRuleId | int | No | PK | IDENTITY(1,1) | Primary key |
| SourceClientId | int | No | Logical reference → Clients.ClientId | | Client that owns the source form |
| SourceFormTypeId | int | No | Logical reference → FormTypes.FormTypeId | | Form type to mirror from |
| TargetClientId | int | No | Logical reference → Clients.ClientId | | Client that receives the mirrored form |
| TargetFormTypeId | int | No | Logical reference → FormTypes.FormTypeId | | Form type to mirror to |
| TargetPlaceholderOrganisationId | int | No | FK → Organisations.OrganisationId | | Placeholder organisation to assign mirrored forms to |
| IsActive | bit | No | | 1 | Enables or disables the mirroring rule |

### IntegrationInbox
For storing details of changes to rows in certain tables that can be read by a background process to maybe call 3rd party APIs with the changes.

| Column | Type | Nullable | Key | Default | Notes |
|------|------|------|------|------|------|
| Id | bigint | No | PK | IDENTITY(1,1) | Primary key |
| ClientId | int | No | | | Client owner |
| EntityType | nvarchar(100) | No | | | Source entity/table name |
| EventType | nvarchar(100) | No | | | Event category for downstream handling |
| ChangeType | nvarchar(50) | No | | | Type of change captured for the event |
| ExternalEntityId | nvarchar(100) | Yes | | | External system identifier when applicable |
| PayloadJson | nvarchar(max) | No | | | Raw payload captured for downstream processing |
| ReceivedUtc | datetime2 | No | | SYSUTCDATETIME() | When the change was received |
| ProcessedUtc | datetime2 | Yes | | | When downstream processing completed |

### IntegrationOutbound
For storing outbound integration events and payloads that are ready to be processed or have been sent to external systems.

| Column | Type | Nullable | Key | Default | Notes |
|------|------|------|------|------|------|
| Id | bigint | No | PK | IDENTITY(1,1) | Primary key |
| ClientId | int | No | | | Client owner |
| EntityType | nvarchar(100) | No | | | Source entity/table name |
| EventType | nvarchar(100) | No | | | Event category for downstream handling |
| ChangeType | nvarchar(50) | No | | | Type of change captured for the event |
| ExternalEntityId | nvarchar(100) | Yes | | | External system identifier when applicable |
| PayloadJson | nvarchar(max) | No | | | Raw payload captured for downstream processing |
| ReceivedUtc | datetime2 | No | | SYSUTCDATETIME() | When the change was received |
| ProcessedUtc | datetime2 | Yes | | | When downstream processing completed |

### IntegrationInbound
For storing inbound integration events and payloads received from external systems before or after processing.

| Column | Type | Nullable | Key | Default | Notes |
|------|------|------|------|------|------|
| Id | bigint | No | PK | IDENTITY(1,1) | Primary key |
| ClientId | int | No | | | Client owner |
| EntityType | nvarchar(100) | No | | | Source entity/table name |
| EventType | nvarchar(100) | No | | | Event category for downstream handling |
| ChangeType | nvarchar(50) | No | | | Type of change captured for the event |
| ExternalEntityId | nvarchar(100) | Yes | | | External system identifier when applicable |
| PayloadJson | nvarchar(max) | No | | | Raw payload captured for downstream processing |
| ReceivedUtc | datetime2 | No | | SYSUTCDATETIME() | When the change was received |
| ProcessedUtc | datetime2 | Yes | | | When downstream processing completed |


## Relationships
| From | To | Type | Notes |
|---|---|---|---|
| Organisations.ClientId | Clients.ClientId | Many-to-One | An organisation belongs to one client |
| Contacts.ClientId | Clients.ClientId | Many-to-One | A contact belongs to one client |
| Contacts.OrganisationId | Organisations.OrganisationId | Many-to-One | A contact optionally belongs to one organisation |
| FormTypes.ClientId | Clients.ClientId | Many-to-One | A form type belongs to one client |
| Forms.ClientId | Clients.ClientId | Many-to-One | A form belongs to one client |
| Forms.FormTypeId | FormTypes.FormTypeId | Many-to-One | A form references one form type definition |
| Forms.OrganisationId | Organisations.OrganisationId | Many-to-One | A form optionally belongs to one organisation |
| Forms.ContactId | Contacts.ContactId | Many-to-One | A form optionally belongs to one contact |

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
