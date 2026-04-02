using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace KulaHub.Functions;

public sealed class DispatchIntegrationOutboundFunction(
    IntegrationProcessingService processingService,
    ILogger<DispatchIntegrationOutboundFunction> logger)
{
    [Function("DispatchIntegrationOutbound")]
    public async Task RunAsync([TimerTrigger("15 */1 * * * *")] TimerInfo timerInfo, CancellationToken cancellationToken)
    {
        var dispatchedCount = await processingService.DispatchOutboundAsync(cancellationToken);
        logger.LogInformation("Dispatched {DispatchedCount} IntegrationOutbound entries at {UtcNow}.", dispatchedCount, DateTime.UtcNow);
    }
}