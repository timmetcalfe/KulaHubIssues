using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace KulaHub.Functions.Clients.Southbridge;

public sealed class SouthbridgeInboundConsumerFunction(
    IntegrationProcessingService processingService,
    ILogger<SouthbridgeInboundConsumerFunction> logger)
{
    [Function("SouthbridgeInboundConsumer")]
    public async Task RunAsync(
        [ServiceBusTrigger(ProcessingOptions.SouthbridgeInboundQueueSetting, Connection = "ServiceBusConnection")]
        string message,
        CancellationToken cancellationToken)
    {
        var payload = IntegrationFunctionMessageSerializer.Deserialize(message);
        await processingService.CompleteInboundAsync(payload.IntegrationEntryId, cancellationToken);
        logger.LogInformation("Completed inbound integration entry {IntegrationEntryId} for client {ClientId}.", payload.IntegrationEntryId, payload.ClientId);
    }
}