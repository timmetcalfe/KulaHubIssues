# Azure Deployment Plan

## Status

Completed

## Goal

Add Azure Application Insights telemetry to `src/KulaHub.Web` and `src/KulaHub.Functions` using the same Application Insights connection string so telemetry can be observed across the web app, API, and Functions services.

## Change Scope

1. Add `Azure.Monitor.OpenTelemetry.AspNetCore` to `src/KulaHub.Web/KulaHub.Web.csproj`.
2. Register Azure Monitor OpenTelemetry in `src/KulaHub.Web/Program.cs`.
3. Add an `AzureMonitor:ConnectionString` entry to `src/KulaHub.Web/appsettings.Development.json` using the same connection string already used by the API.
4. Migrate `src/KulaHub.Functions` from the current classic worker Application Insights setup to the official .NET isolated OpenTelemetry setup using `Microsoft.Azure.Functions.Worker.OpenTelemetry` and `Azure.Monitor.OpenTelemetry.Exporter`.
5. Enable OpenTelemetry in `src/KulaHub.Functions/host.json` and add `APPLICATIONINSIGHTS_CONNECTION_STRING` to `src/KulaHub.Functions/local.settings.json`.
6. Preserve environment-variable based production configuration for Azure-hosted services.
7. Validate the web and functions projects build after the change.

## Notes

- Official Azure guidance recommends environment variables for production connection strings.
- For Azure Functions .NET isolated, Microsoft guidance recommends enabling OpenTelemetry in `host.json` and instrumenting the worker with `Microsoft.Azure.Functions.Worker.OpenTelemetry` plus the Azure Monitor exporter.
- This plan avoids mixing the Azure Monitor ASP.NET Core distro into the Functions worker because Microsoft guidance warns that can cause duplicate request telemetry.

## Execution Steps

1. Update the web project package reference and startup configuration.
2. Add the shared Application Insights connection string to web development configuration.
3. Replace the current Functions worker Application Insights registration with OpenTelemetry-based registration.
4. Update Functions configuration for OpenTelemetry and the shared connection string.
5. Build `KulaHub.Web` and `KulaHub.Functions` to confirm the changes are valid.