# Azure Plan

## Status

In Progress

## Goal

Prepare and implement a .NET 10 multi-project CRM sample for Azure App Service, Azure Functions, Azure SQL, and Azure Service Bus.

## Architecture

- Azure SQL for the primary relational data store.
- ASP.NET Core Web API hosted on Azure App Service.
- ASP.NET Core Razor Pages web application hosted on Azure App Service.
- One Azure Functions app for timer-triggered and Service Bus-triggered background processing.
- Azure Service Bus queues for tenant-specific inbound and outbound processing.
- AZD plus Bicep for environment and infrastructure provisioning.

## Scope Decisions

- Include Notes in the first version.
- Use .NET 10 for all new projects.
- Keep authentication out of scope for the first version.
- Use direct queue routing for outbound events when a rule resolves a single destination.
- Use ID-based queue names such as `clientid4-outbound`.
- Seed sample routing around Dealer (ClientId 4) and Polaris Advisory (ClientId 3).

## Current Execution Plan

1. Update the SQL schema and documentation to match the approved model.
2. Scaffold the .NET 10 solution and projects.
3. Add shared data access, configuration, and messaging abstractions.
4. Implement the first end-to-end contact and note slice.
5. Prepare Bicep and AZD assets after the application shape is stable.
