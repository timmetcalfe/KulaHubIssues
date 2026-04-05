using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace KulaHub.Functions;

public sealed class DispatchIntegrationDispatchFunction(
    IntegrationProcessingService processingService,
    ILogger<DispatchIntegrationDispatchFunction> logger)
{
    [Function("DispatchIntegrationDispatch")]
    public async Task RunAsync([TimerTrigger("15 */1 * * * *")] TimerInfo timerInfo, CancellationToken cancellationToken)
    {
        var dispatchedCount = await processingService.DispatchAsync(cancellationToken);
        logger.LogInformation("Dispatched {DispatchedCount} IntegrationDispatch entries at {UtcNow}.", dispatchedCount, DateTime.UtcNow);
    }
}