# Data Model
This document describes the data models used in KulaHub.

## Overview
This system supports clients(tenants), organisations, contacts and forms for a multi-tenant SaaS CRM application.

**Conventions and assumptions**
- All tables use `Id` as PK unless otherwise stated
- All client-owned tables include `ClientId`
- Soft delete uses `DeletedUtc`
- Audit fields: `CreatedUtc`, `CreatedBy`, `ModifiedUtc`, `ModifiedBy`. Automatically include these when creating new tables.

## Entities

### Clients
Represents the organisations that are using the system. Each client will have many organisations, contacts and forms

| Column | Type | Nullable | Key | Default | Notes |
|------|------|------|------|------|------|
| Id | uniqueidentifier | No | PK | | Primary key |
| Name | nvarchar(200) | No | | | Display name |
| Postcode | nvarchar(12) | Yes | | | |

### Organisations
Reprsent the organisations that are a client is dealing with.

| Column | Type | Nullable | Key | Default | Notes |
|------|------|------|------|------|------|
| Id | uniqueidentifier | No | PK | | Primary key |
| ClientId | uniqueidentifier | No | FK → Clients.Id | | Client owner |
| Name | nvarchar(200) | No | | | Display name |
| Postcode | nvarchar(12) | Yes | | | |
