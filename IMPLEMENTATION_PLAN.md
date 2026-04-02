# KulaHub Implementation Plan

## Summary

Build the solution in phases around a shared EF Core data layer, three host applications (Web API, Razor Pages web app, one Azure Functions app), and Azure deployment assets using AZD plus Bicep. The first version targets .NET 10, includes Notes, keeps authentication out of scope, uses a tenant-switching Razor UI that can create contacts and add forms and notes, and implements a reliable event pipeline that separates dispatch state from final completion state for IntegrationOutbound and IntegrationInbound.

## Decisions

- Target .NET 10 for all new projects.
- Include Notes in the first version.
- Include Azure infrastructure and deployment assets in scope.
- Keep authentication out of scope for the first version.
- Use one Azure Functions project with multiple triggers rather than several separate Function App projects.
- Keep the Web API minimal in the first version instead of full CRUD for every entity.
- Allow contact creation in the Razor Pages UI.
- Add separate dispatch and completion state for IntegrationOutbound and IntegrationInbound.
- Use configuration-driven sample routing rules.
- Keep the existing tenant records as Northwind Advisory (ClientId 3) and Southbridge Retail (ClientId 4).
- Keep queue names ID-based, for example `clientid4-outbound`.
- Route outbound events directly to the target queue when the rule determines a single destination.

## Phases

1. Stabilize the specification and database contract.
   Add a Notes table, document Notes in the data model, reconcile Forms schema drift, and extend IntegrationOutbound and IntegrationInbound with separate dispatch and completion state.
2. Define the solution topology.
   Create a .NET 10 solution with shared data/application code, a Web API, a Razor Pages app, one Azure Functions app, and tests.
3. Implement the shared data/application layer.
   Build the EF Core model against Azure SQL and centralize write operations so business changes and IntegrationInbox inserts happen atomically.
4. Implement the minimal Web API.
   Support creating contacts, listing contacts, getting contact details, and adding notes.
5. Implement the Razor Pages app.
   Support tenant switching, contact list/detail, contact creation, note creation, form creation, and viewing related forms and notes.
6. Implement background processing.
   Add timer-triggered processing for IntegrationInbox, IntegrationOutbound, and IntegrationInbound plus Service Bus-triggered handlers for sample client-specific consumers.
7. Implement sample routing behavior.
   Use config-driven rules seeded around Southbridge Retail (ClientId 4) outbound and Northwind Advisory (ClientId 3) inbound, with direct queue routing for outbound processing.
8. Prepare Azure deployment assets.
   Create AZD and Bicep assets for Azure SQL, Service Bus, one Function App, one Web API host, and one Razor web app host.
9. Add seed data and local developer workflow.
   Make local execution and integration testing practical with documented startup/configuration.
10. Verify the vertical slice.
   Prove transactional inbox creation, queue dispatch, queue consumption, and tenant-isolated user flows.

## First Implementation Slice

The initial implementation work starts with Phase 1 and the minimum scaffolding needed for later phases:

- Update the database schema and data model documentation.
- Align seed data with the renamed tenants.
- Create the .NET 10 solution and initial projects.
- Add shared configuration and package references.

## Verification Targets

1. SQL project builds after schema changes.
2. Contact and note writes persist both the domain row and IntegrationInbox row in one transaction.
3. Background processing can route to the correct queue without duplicate dispatch.
4. Razor Pages flows work for tenant switching, contact creation, and note/form creation.
5. The minimal API works for contact create/list/detail and note creation.
