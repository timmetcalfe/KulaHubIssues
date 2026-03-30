using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace KulaHub.Functions;

public sealed class SouthbridgeOutboundConsumerFunction(
    IntegrationProcessingService processingService,
    ILogger<SouthbridgeOutboundConsumerFunction> logger)
{
    [Function("SouthbridgeOutboundConsumer")]
    public async Task RunAsync(
        [ServiceBusTrigger(ProcessingOptions.SouthbridgeOutboundQueueSetting, Connection = "ServiceBusConnection")]
        string message,
        CancellationToken cancellationToken)
    {
        var payload = IntegrationFunctionMessageSerializer.Deserialize(message);
        await processingService.CompleteOutboundAsync(payload.IntegrationEntryId, cancellationToken);
        logger.LogInformation("Completed outbound integration entry {IntegrationEntryId} for client {ClientId}.", payload.IntegrationEntryId, payload.ClientId);
    }
}