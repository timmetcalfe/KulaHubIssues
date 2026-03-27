using KulaHub.Data;
using KulaHub.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KulaHub.Functions;

public sealed class IntegrationProcessingService(
    KulaHubDbContext dbContext,
    IQueueMessageSender queueMessageSender,
    IOptions<ProcessingOptions> options,
    ILogger<IntegrationProcessingService> logger)
{
    private readonly ProcessingOptions processingOptions = options.Value;

    public async Task<int> ProcessInboxAsync(CancellationToken cancellationToken)
    {
        var inboxBatch = await dbContext.IntegrationInbox
            .Where(entry => entry.ProcessedUtc == null)
            .OrderBy(entry => entry.ReceivedUtc)
            .Take(processingOptions.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var entry in inboxBatch)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var processedUtc = DateTime.UtcNow;

            if (entry.ClientId == processingOptions.SouthbridgeOutboundClientId)
            {
                dbContext.IntegrationOutbound.Add(new IntegrationOutboundEntry
                {
                    ClientId = entry.ClientId,
                    EntityType = entry.EntityType,
                    EventType = entry.EventType,
                    ChangeType = entry.ChangeType,
                    ExternalEntityId = entry.ExternalEntityId,
                    PayloadJson = entry.PayloadJson,
                    ReceivedUtc = entry.ReceivedUtc
                });
            }
            else if (entry.ClientId == processingOptions.NorthwindInboundClientId)
            {
                dbContext.IntegrationInbound.Add(new IntegrationInboundEntry
                {
                    ClientId = entry.ClientId,
                    EntityType = entry.EntityType,
                    EventType = entry.EventType,
                    ChangeType = entry.ChangeType,
                    ExternalEntityId = entry.ExternalEntityId,
                    PayloadJson = entry.PayloadJson,
                    ReceivedUtc = entry.ReceivedUtc
                });
            }
            else
            {
                logger.LogInformation("IntegrationInbox entry {IntegrationInboxId} for client {ClientId} has no matching processing rule.", entry.Id, entry.ClientId);
            }

            entry.ProcessedUtc = processedUtc;
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        return inboxBatch.Count;
    }

    public async Task<int> DispatchOutboundAsync(CancellationToken cancellationToken)
    {
        var outboundBatch = await dbContext.IntegrationOutbound
            .Where(entry => entry.DispatchedUtc == null && entry.ProcessedUtc == null)
            .OrderBy(entry => entry.ReceivedUtc)
            .Take(processingOptions.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var entry in outboundBatch)
        {
            var queueName = ResolveOutboundQueue(entry.ClientId);
            if (queueName is null)
            {
                entry.ProcessedUtc = DateTime.UtcNow;
                entry.DispatchTarget = "ignored";
                logger.LogInformation("IntegrationOutbound entry {IntegrationOutboundId} was ignored because no outbound queue rule matched client {ClientId}.", entry.Id, entry.ClientId);
                await dbContext.SaveChangesAsync(cancellationToken);
                continue;
            }

            var message = new QueuedIntegrationMessage(entry.Id, entry.ClientId, entry.EntityType, entry.EventType, entry.ChangeType, entry.PayloadJson, queueName);
            await queueMessageSender.SendAsync(queueName, message, cancellationToken);

            entry.DispatchTarget = queueName;
            entry.DispatchedUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return outboundBatch.Count;
    }

    public async Task<int> DispatchInboundAsync(CancellationToken cancellationToken)
    {
        var inboundBatch = await dbContext.IntegrationInbound
            .Where(entry => entry.DispatchedUtc == null && entry.ProcessedUtc == null)
            .OrderBy(entry => entry.ReceivedUtc)
            .Take(processingOptions.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var entry in inboundBatch)
        {
            var queueName = ResolveInboundQueue(entry.ClientId);
            if (queueName is null)
            {
                entry.ProcessedUtc = DateTime.UtcNow;
                entry.DispatchTarget = "ignored";
                logger.LogInformation("IntegrationInbound entry {IntegrationInboundId} was ignored because no inbound queue rule matched client {ClientId}.", entry.Id, entry.ClientId);
                await dbContext.SaveChangesAsync(cancellationToken);
                continue;
            }

            var message = new QueuedIntegrationMessage(entry.Id, entry.ClientId, entry.EntityType, entry.EventType, entry.ChangeType, entry.PayloadJson, queueName);
            await queueMessageSender.SendAsync(queueName, message, cancellationToken);

            entry.DispatchTarget = queueName;
            entry.DispatchedUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return inboundBatch.Count;
    }

    public async Task CompleteOutboundAsync(long integrationEntryId, CancellationToken cancellationToken)
    {
        var entry = await dbContext.IntegrationOutbound.SingleOrDefaultAsync(item => item.Id == integrationEntryId, cancellationToken)
            ?? throw new KeyNotFoundException($"IntegrationOutbound entry {integrationEntryId} was not found.");

        entry.ProcessedUtc ??= DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteInboundAsync(long integrationEntryId, CancellationToken cancellationToken)
    {
        var entry = await dbContext.IntegrationInbound.SingleOrDefaultAsync(item => item.Id == integrationEntryId, cancellationToken)
            ?? throw new KeyNotFoundException($"IntegrationInbound entry {integrationEntryId} was not found.");

        entry.ProcessedUtc ??= DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private string? ResolveOutboundQueue(int clientId)
    {
        return clientId == processingOptions.SouthbridgeOutboundClientId
            ? processingOptions.SouthbridgeOutboundQueueName
            : null;
    }

    private string? ResolveInboundQueue(int clientId)
    {
        return clientId == processingOptions.NorthwindInboundClientId
            ? processingOptions.NorthwindInboundQueueName
            : null;
    }
}