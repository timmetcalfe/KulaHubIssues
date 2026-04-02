using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace KulaHub.Functions;

public sealed class ProcessIntegrationInboxFunction(
    IntegrationProcessingService processingService,
    ILogger<ProcessIntegrationInboxFunction> logger)
{
    [Function("ProcessIntegrationInbox")]
    public async Task RunAsync([TimerTrigger("0 */1 * * * *")] TimerInfo timerInfo, CancellationToken cancellationToken)
    {
        var processedCount = await processingService.ProcessInboxAsync(cancellationToken);
        logger.LogInformation("Processed {ProcessedCount} IntegrationInbox entries at {UtcNow}.", processedCount, DateTime.UtcNow);
    }
}