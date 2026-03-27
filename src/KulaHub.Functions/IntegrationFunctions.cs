using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace KulaHub.Functions;

public sealed class IntegrationFunctions(IntegrationProcessingService processingService, ILogger<IntegrationFunctions> logger)
{
    [Function("ProcessIntegrationInbox")]
    public async Task ProcessIntegrationInboxAsync([TimerTrigger("0 */1 * * * *")] TimerInfo timerInfo, CancellationToken cancellationToken)
    {
        var processedCount = await processingService.ProcessInboxAsync(cancellationToken);
        logger.LogInformation("Processed {ProcessedCount} IntegrationInbox entries at {UtcNow}.", processedCount, DateTime.UtcNow);
    }

    [Function("DispatchIntegrationOutbound")]
    public async Task DispatchIntegrationOutboundAsync([TimerTrigger("15 */1 * * * *")] TimerInfo timerInfo, CancellationToken cancellationToken)
    {
        var dispatchedCount = await processingService.DispatchOutboundAsync(cancellationToken);
        logger.LogInformation("Dispatched {DispatchedCount} IntegrationOutbound entries at {UtcNow}.", dispatchedCount, DateTime.UtcNow);
    }

    [Function("DispatchIntegrationInbound")]
    public async Task DispatchIntegrationInboundAsync([TimerTrigger("30 */1 * * * *")] TimerInfo timerInfo, CancellationToken cancellationToken)
    {
        var dispatchedCount = await processingService.DispatchInboundAsync(cancellationToken);
        logger.LogInformation("Dispatched {DispatchedCount} IntegrationInbound entries at {UtcNow}.", dispatchedCount, DateTime.UtcNow);
    }

    [Function("SouthbridgeOutboundConsumer")]
    public async Task SouthbridgeOutboundConsumerAsync(
        [ServiceBusTrigger("%ProcessingOptions__SouthbridgeOutboundQueueName%", Connection = "ServiceBusConnection")]
        string message,
        CancellationToken cancellationToken)
    {
        var payload = Deserialize(message);
        await processingService.CompleteOutboundAsync(payload.IntegrationEntryId, cancellationToken);
        logger.LogInformation("Completed outbound integration entry {IntegrationEntryId} for client {ClientId}.", payload.IntegrationEntryId, payload.ClientId);
    }

    [Function("NorthwindInboundConsumer")]
    public async Task NorthwindInboundConsumerAsync(
        [ServiceBusTrigger("%ProcessingOptions__NorthwindInboundQueueName%", Connection = "ServiceBusConnection")]
        string message,
        CancellationToken cancellationToken)
    {
        var payload = Deserialize(message);
        await processingService.CompleteInboundAsync(payload.IntegrationEntryId, cancellationToken);
        logger.LogInformation("Completed inbound integration entry {IntegrationEntryId} for client {ClientId}.", payload.IntegrationEntryId, payload.ClientId);
    }

    private static QueuedIntegrationMessage Deserialize(string message)
    {
        return JsonSerializer.Deserialize<QueuedIntegrationMessage>(message)
            ?? throw new InvalidOperationException("The integration message payload could not be deserialized.");
    }
}