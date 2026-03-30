using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace KulaHub.Functions;

public sealed class NorthwindInboundConsumerFunction(
    IntegrationProcessingService processingService,
    ILogger<NorthwindInboundConsumerFunction> logger)
{
    [Function("NorthwindInboundConsumer")]
    public async Task RunAsync(
        [ServiceBusTrigger(ProcessingOptions.NorthwindInboundQueueSetting, Connection = "ServiceBusConnection")]
        string message,
        CancellationToken cancellationToken)
    {
        var payload = IntegrationFunctionMessageSerializer.Deserialize(message);
        await processingService.CompleteInboundAsync(payload.IntegrationEntryId, cancellationToken);
        logger.LogInformation("Completed inbound integration entry {IntegrationEntryId} for client {ClientId}.", payload.IntegrationEntryId, payload.ClientId);
    }
}