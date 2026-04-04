using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace KulaHub.Functions.Clients.Polaris;

public sealed class PolarisOutboundConsumerFunction(
    IntegrationProcessingService processingService,
    IHttpClientFactory httpClientFactory,
    ILogger<PolarisOutboundConsumerFunction> logger)
{
    [Function("PolarisOutboundConsumer")]
    public async Task RunAsync(
        [ServiceBusTrigger(ProcessingOptions.PolarisOutboundQueueSetting, Connection = "ServiceBusConnection")]
        string message,
        CancellationToken cancellationToken)
    {
        var payload = IntegrationFunctionMessageSerializer.Deserialize(message);

        var httpClient = httpClientFactory.CreateClient("PolarisOutboundHttpClient");
        using var response = await httpClient.PostAsJsonAsync(
            "anything",
            new
            {
                payload.IntegrationEntryId,
                payload.ClientId,
                payload.EntityType,
                payload.EventType,
                payload.ChangeType,
                payload.PayloadJson,
                ReceivedMessage = message
            },
            cancellationToken);

        response.EnsureSuccessStatusCode();
        logger.LogInformation(
            "Posted outbound integration entry {IntegrationEntryId} for client {ClientId} to httpbin with status code {StatusCode}.",
            payload.IntegrationEntryId,
            payload.ClientId,
            (int)response.StatusCode);
        await processingService.CompleteOutboundAsync(payload.IntegrationEntryId, cancellationToken);
        logger.LogInformation("Completed outbound integration entry {IntegrationEntryId} for client {ClientId}.", payload.IntegrationEntryId, payload.ClientId);
    }
}