using KulaHub.Data;
using KulaHub.Functions;
using Microsoft.Extensions.Azure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

var serviceBusConnectionString = builder.Configuration["ServiceBusConnection"];

if (string.IsNullOrWhiteSpace(serviceBusConnectionString))
{
    throw new InvalidOperationException("The ServiceBusConnection setting is required to dispatch integration messages.");
}

builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddServiceBusClient(serviceBusConnectionString);
});

builder.Services.AddKulaHubData(builder.Configuration);
builder.Services.Configure<ProcessingOptions>(builder.Configuration.GetSection(ProcessingOptions.SectionName));
builder.Services.AddScoped<IntegrationProcessingService>();
builder.Services.AddSingleton<IQueueMessageSender, QueueMessageSender>();

builder.Build().Run();
