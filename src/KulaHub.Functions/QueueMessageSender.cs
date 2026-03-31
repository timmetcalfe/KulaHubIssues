using System.Text.Json;
using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;

namespace KulaHub.Functions;

public interface IQueueMessageSender
{
    Task SendAsync(string queueName, QueuedIntegrationMessage message, CancellationToken cancellationToken);
}

public sealed class QueueMessageSender(ServiceBusClient client) : IQueueMessageSender, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, ServiceBusSender> senders = new(StringComparer.OrdinalIgnoreCase);

    public async Task SendAsync(string queueName, QueuedIntegrationMessage message, CancellationToken cancellationToken)
    {
        var sender = senders.GetOrAdd(queueName, client.CreateSender);
        var serviceBusMessage = new ServiceBusMessage(BinaryData.FromString(JsonSerializer.Serialize(message)))
        {
            MessageId = $"{message.IntegrationEntryId}:{message.EventType}",
            Subject = message.EventType
        };

        await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
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