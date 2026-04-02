# Azure Deployment Plan

## Status

Completed

## Goal

Add Azure Application Insights telemetry to `src/KulaHub.Api` using `Azure.Monitor.OpenTelemetry.AspNetCore` and configure the provided connection string for local and configuration-based use.

## Change Scope

1. Add the `Azure.Monitor.OpenTelemetry.AspNetCore` package to `src/KulaHub.Api/KulaHub.Api.csproj`.
2. Register Azure Monitor OpenTelemetry in `src/KulaHub.Api/Program.cs`.
3. Add an `AzureMonitor:ConnectionString` configuration entry to `src/KulaHub.Api/appsettings.Development.json` using the provided value.
4. Preserve support for environment-variable overrides such as `APPLICATIONINSIGHTS_CONNECTION_STRING`.
5. Validate the API project builds after the change.

## Notes

- Official Azure guidance recommends using an environment variable for production connection strings.
- The requested change will keep the provided connection string in development configuration for this repo unless a different storage location is requested.

## Execution Steps

1. Update the API project package reference.
2. Wire `AddOpenTelemetry().UseAzureMonitor()` into API startup.
3. Add the Azure Monitor connection string to development configuration.
4. Build the API project to confirm the change is valid.