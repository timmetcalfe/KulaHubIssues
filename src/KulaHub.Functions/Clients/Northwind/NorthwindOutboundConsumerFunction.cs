using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace KulaHub.Functions.Clients.Northwind;

public sealed class NorthwindOutboundConsumerFunction(
    IntegrationProcessingService processingService,
    ILogger<NorthwindOutboundConsumerFunction> logger)
{
    [Function("NorthwindOutboundConsumer")]
    public async Task RunAsync(
        [ServiceBusTrigger(ProcessingOptions.NorthwindOutboundQueueSetting, Connection = "ServiceBusConnection")]
        string message,
        CancellationToken cancellationToken)
    {
        var payload = IntegrationFunctionMessageSerializer.Deserialize(message);
        await processingService.CompleteOutboundAsync(payload.IntegrationEntryId, cancellationToken);
        logger.LogInformation("Completed outbound integration entry {IntegrationEntryId} for client {ClientId}.", payload.IntegrationEntryId, payload.ClientId);
    }
}