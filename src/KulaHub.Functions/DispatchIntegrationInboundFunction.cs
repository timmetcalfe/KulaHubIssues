using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace KulaHub.Functions;

public sealed class DispatchIntegrationInboundFunction(
    IntegrationProcessingService processingService,
    ILogger<DispatchIntegrationInboundFunction> logger)
{
    [Function("DispatchIntegrationInbound")]
    public async Task RunAsync([TimerTrigger("30 */1 * * * *")] TimerInfo timerInfo, CancellationToken cancellationToken)
    {
        var dispatchedCount = await processingService.DispatchInboundAsync(cancellationToken);
        logger.LogInformation("Dispatched {DispatchedCount} IntegrationInbound entries at {UtcNow}.", dispatchedCount, DateTime.UtcNow);
    }
}