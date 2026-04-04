using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace KulaHub.Functions.Clients.Northwind;

public sealed class NorthwindOutboundConsumerFunction(
    IntegrationProcessingService processingService,
    IHttpClientFactory httpClientFactory,
    ILogger<NorthwindOutboundConsumerFunction> logger)
{
    [Function("NorthwindOutboundConsumer")]
    public async Task RunAsync(
        [ServiceBusTrigger(ProcessingOptions.NorthwindOutboundQueueSetting, Connection = "ServiceBusConnection")]
        string message,
        CancellationToken cancellationToken)
    {
        var payload = IntegrationFunctionMessageSerializer.Deserialize(message);

        var httpClient = httpClientFactory.CreateClient("NorthwindOutboundHttpClient");
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