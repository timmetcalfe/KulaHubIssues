using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace KulaHub.Functions.Clients.Dealer;

public sealed class DealerInternalConsumerFunction(
    DealerContactMirrorService contactMirrorService,
    IntegrationProcessingService processingService,
    ILogger<DealerInternalConsumerFunction> logger)
{
    [Function("DealerInternalConsumer")]
    public async Task RunAsync(
        [ServiceBusTrigger(ProcessingOptions.DealerInboundQueueSetting, Connection = "ServiceBusConnection")]
        string message,
        CancellationToken cancellationToken)
    {
        var payload = IntegrationFunctionMessageSerializer.Deserialize(message);
        await contactMirrorService.MirrorToPolarisIfRequiredAsync(payload, cancellationToken);
        await processingService.CompleteInboundAsync(payload.IntegrationEntryId, cancellationToken);
        logger.LogInformation("Completed inbound integration entry {IntegrationEntryId} for client {ClientId}.", payload.IntegrationEntryId, payload.ClientId);
    }
}