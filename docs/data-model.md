# Data Model

This document explains the intent of the KulaHub data model.

## Source of truth

The deployable database schema lives in the SQL project under the `Database` folder. Treat the SQL project as the authoritative source for tables, columns, indexes, constraints, and defaults.

Relevant schema files include:

- `Database/Clients.sql`
- `Database/Organisations.sql`
- `Database/Contacts.sql`
- `Database/Notes.sql`
- `Database/FormTypes.sql`
- `Database/Forms.sql`
- `Database/FormMirrorRules.sql`
- `Database/IntegrationInbox.sql`
- `Database/IntegrationDispatch.sql`

Use this markdown file for:

- business meaning of each entity
- tenancy and ownership rules
- integration flow semantics
- notable invariants that are easy to miss when reading raw DDL

Do not duplicate full column-by-column schema definitions here unless there is a strong reason to explain a specific field.

## Overview

KulaHub is a multi-tenant CRM. The core model supports clients, organisations, contacts, notes, forms, and integration work items used to route changes to downstream processing.

## Cross-cutting conventions

- Azure SQL is the target database engine.
- Most business tables use an `EntityId` identity primary key named after the entity, for example `ContactId` or `FormId`.
- Client-owned data carries `ClientId` to enforce tenant ownership.
- Most business tables support soft delete via `DeletedUtc`.
- Most business tables include audit fields: `CreatedUtc`, `CreatedBy`, `ModifiedUtc`, `ModifiedBy`.
- Integration tables are operational event tables and do not fully follow the same audit conventions as CRM entities.

## Core CRM entities

### Clients

Represents a tenant using the CRM system. A client owns organisations, contacts, form types, forms, and integration records.

### Organisations

Represents an organisation that belongs to a client. Organisations group contacts and can also own forms.

### Contacts

Represents a person within a client tenant. A contact may optionally belong to an organisation.

`SourceContactId` is a logical self-reference used when one client mirrors a contact from another workflow, such as Dealer to Polaris synchronisation.

### Notes

Represents free-text notes attached to a contact. Notes are client-owned and always associated with a contact.

### FormTypes

Represents a client-defined form definition. A form type describes the kind of form a tenant can attach to contacts or organisations.

### Forms

Represents a concrete form instance. A form belongs to one client and one form type, and it must be attached to either:

- an organisation, or
- a contact

The SQL schema enforces that a form cannot exist without at least one owner.

`OriginalFormId` is a logical self-reference used to track mirrored copies of a source form.

### FormMirrorRules

Represents rules for mirroring forms between clients. This area is currently present in the schema but not yet a primary implementation focus.

## Integration entities

### Feedback
Stores feedback submitted via the Feedback page form.

| Column | Type | Nullable | Key | Default | Notes |
|------|------|------|------|------|------|
| FeedbackId | int | No | PK | IDENTITY(1,1) | Primary key |
| Name | nvarchar(100) | No | | | Submitter's name |
| Email | nvarchar(200) | No | | | Submitter's email address |
| Comments | nvarchar(max) | No | | | Free-text feedback comments |
| CreatedUtc | datetime2 | No | | | When the feedback was submitted |
| CreatedBy | nvarchar(100) | No | | | Origin of submission |
| ModifiedUtc | datetime2 | Yes | | | |
| ModifiedBy | nvarchar(100) | Yes | | | |
| DeletedUtc | datetime2 | Yes | | | Soft delete timestamp |

### IntegrationInbox

Stores captured change events before the system decides what to do with them. This is the intake point for integration processing.

Important concepts:

- `OriginType` distinguishes internal versus external origin.
- `SourceSystemKey` supports routing and loop prevention.
- `CorrelationId` and `TraceParent` preserve distributed tracing context.
- `ProcessedUtc` marks whether the event has already been classified.

### IntegrationDispatch

Stores routed work items that are ready for queue dispatch or have already been processed by downstream consumers.

Important concepts:

- `IntegrationInboxId` links dispatch work back to the originating inbox event.
- `Disposition` captures the routing class, such as inbound or outbound.
- `QueueKey` is the logical routing output used to resolve the actual queue.
- `DispatchedUtc` marks queue publication.
- `ProcessedUtc` marks downstream completion.

## Relationship summary

- A client owns many organisations.
- A client owns many contacts.
- An organisation optionally owns many contacts.
- A client owns many notes.
- A contact owns many notes.
- A client owns many form types.
- A client owns many forms.
- A form type owns many forms.
- An organisation may own many forms.
- A contact may own many forms.
- An inbox event may produce zero or more dispatch records.

## Operational notes

- The SQL schema includes indexes and constraints that are intentionally not repeated here. Check the SQL files when changing query patterns or deployment shape.
- Filtered indexes exist on some nullable lookup fields and on unprocessed integration work to support polling functions efficiently.
- Some relationships are kept as logical references in the integration model instead of enforced foreign keys, which keeps ingestion and dispatch flows tolerant of external or staged data.

## Change policy

When changing the data model:

1. Update the SQL project first.
2. Update this document only if the business meaning, workflow semantics, or important invariants have changed.
3. Do not restate the full schema here unless the explanation adds architectural value.
