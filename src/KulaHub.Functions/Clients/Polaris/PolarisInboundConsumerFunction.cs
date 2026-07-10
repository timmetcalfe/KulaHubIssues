using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace KulaHub.Functions.Clients.Polaris;

public sealed class PolarisInboundConsumerFunction(
    IntegrationProcessingService processingService,
    ILogger<PolarisInboundConsumerFunction> logger)
{
    [Function("PolarisInboundConsumer")]
    public async Task RunAsync(
        [ServiceBusTrigger(ProcessingOptions.PolarisImportProcessingQueueSetting, Connection = "ServiceBusConnection")]
        string message,
        CancellationToken cancellationToken)
    {
        var payload = IntegrationFunctionMessageSerializer.Deserialize(message);
        await processingService.CompleteDispatchAsync(payload.IntegrationEntryId, cancellationToken);
        logger.LogInformation("Completed integration dispatch entry {IntegrationEntryId} for client {ClientId}.", payload.IntegrationEntryId, payload.ClientId);
    }
}