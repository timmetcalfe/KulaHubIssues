using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;

namespace KulaHub.Functions;

public interface IQueueMessageSender
{
    Task SendAsync(string queueName, QueuedIntegrationMessage message, CancellationToken cancellationToken);
}

public sealed class QueueMessageSender(IConfiguration configuration) : IQueueMessageSender
{
    public async Task SendAsync(string queueName, QueuedIntegrationMessage message, CancellationToken cancellationToken)
    {
        var connectionString = configuration["ServiceBusConnection"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("The ServiceBusConnection setting is required to dispatch integration messages.");
        }

        await using var client = new ServiceBusClient(connectionString);
        var sender = client.CreateSender(queueName);
        var serviceBusMessage = new ServiceBusMessage(BinaryData.FromString(JsonSerializer.Serialize(message)))
        {
            MessageId = $"{message.IntegrationEntryId}:{message.EventType}",
            Subject = message.EventType
        };

        await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
    }
}