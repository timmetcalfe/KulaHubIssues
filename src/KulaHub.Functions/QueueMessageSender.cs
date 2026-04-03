using System.Diagnostics;
using System.Text.Json;
using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;

namespace KulaHub.Functions;

public interface IQueueMessageSender
{
    Task SendAsync(string queueName, QueuedIntegrationMessage message, string? traceParent, string? correlationId, CancellationToken cancellationToken);
}

public sealed class QueueMessageSender(ServiceBusClient client) : IQueueMessageSender, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, ServiceBusSender> senders = new(StringComparer.OrdinalIgnoreCase);

    public async Task SendAsync(string queueName, QueuedIntegrationMessage message, string? traceParent, string? correlationId, CancellationToken cancellationToken)
    {
        var sender = senders.GetOrAdd(queueName, client.CreateSender);
        var serviceBusMessage = new ServiceBusMessage(BinaryData.FromString(JsonSerializer.Serialize(message)))
        {
            MessageId = $"{message.IntegrationEntryId}:{message.EventType}",
            Subject = message.EventType
        };

        var diagnosticId = CreateDiagnosticId(traceParent, correlationId);
        if (diagnosticId is not null)
        {
            serviceBusMessage.ApplicationProperties["Diagnostic-Id"] = diagnosticId;
        }

        await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
    }

    private static string? CreateDiagnosticId(string? traceParent, string? correlationId)
    {
        if (!string.IsNullOrWhiteSpace(traceParent) && ActivityContext.TryParse(traceParent, null, out _))
        {
            return traceParent;
        }

        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length != 32)
        {
            return null;
        }

        try
        {
            var traceId = ActivityTraceId.CreateFromString(correlationId.AsSpan());
            var spanId = ActivitySpanId.CreateRandom();
            return $"00-{traceId}-{spanId}-01";
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var sender in senders.Values)
        {
            await sender.DisposeAsync();
        }

        senders.Clear()
;    }
}