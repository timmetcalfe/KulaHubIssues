using System.Text.Json;

namespace KulaHub.Functions;

internal static class IntegrationFunctionMessageSerializer
{
    public static QueuedIntegrationMessage Deserialize(string message)
    {
        return JsonSerializer.Deserialize<QueuedIntegrationMessage>(message)
            ?? throw new InvalidOperationException("The integration message payload could not be deserialized.");
    }
}