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
            var processedUtc = DateTime.UtcNow;
            var routingRule = ResolveRoutingRule(entry);

            if (routingRule is not null)
            {
                dbContext.IntegrationDispatch.Add(new IntegrationDispatchEntry
                {
                    IntegrationInboxId = entry.Id,
                    CorrelationId = entry.CorrelationId,
                    TraceParent = entry.TraceParent,
                    ClientId = entry.ClientId,
                    Disposition = routingRule.Disposition,
                    OriginType = entry.OriginType,
                    SourceSystemKey = entry.SourceSystemKey,
                    QueueKey = routingRule.QueueKey,
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
                logger.LogInformation("IntegrationInbox entry {IntegrationInboxId} for client {ClientId}, origin type {OriginType}, and source system {SourceSystemKey} has no matching routing rule.", entry.Id, entry.ClientId, entry.OriginType, entry.SourceSystemKey ?? "<none>");
            }

            entry.ProcessedUtc = processedUtc;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return inboxBatch.Count;
    }

    public async Task<int> DispatchAsync(CancellationToken cancellationToken)
    {
        var dispatchBatch = await dbContext.IntegrationDispatch
            .Where(entry => entry.DispatchedUtc == null && entry.ProcessedUtc == null)
            .OrderBy(entry => entry.ReceivedUtc)
            .Take(processingOptions.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var entry in dispatchBatch)
        {
            var queueName = ResolveQueueName(entry.QueueKey);
            if (queueName is null)
            {
                entry.ProcessedUtc = DateTime.UtcNow;
                entry.DispatchTarget = "ignored";
                logger.LogInformation("IntegrationDispatch entry {IntegrationDispatchId} was ignored because queue key {QueueKey} has no configured queue binding.", entry.Id, entry.QueueKey);
                await dbContext.SaveChangesAsync(cancellationToken);
                continue;
            }

            var message = new QueuedIntegrationMessage(entry.Id, entry.ClientId, entry.EntityType, entry.EventType, entry.ChangeType, entry.PayloadJson, queueName, entry.QueueKey, entry.SourceSystemKey);
            await queueMessageSender.SendAsync(queueName, message, entry.TraceParent, entry.CorrelationId, cancellationToken);

            entry.DispatchTarget = queueName;
            entry.DispatchedUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return dispatchBatch.Count;
    }

    public async Task CompleteDispatchAsync(long integrationEntryId, CancellationToken cancellationToken)
    {
        var entry = await dbContext.IntegrationDispatch.SingleOrDefaultAsync(item => item.Id == integrationEntryId, cancellationToken)
            ?? throw new KeyNotFoundException($"IntegrationDispatch entry {integrationEntryId} was not found.");

        entry.ProcessedUtc ??= DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private string? ResolveQueueName(string queueKey)
    {
        return processingOptions.QueueBindings.TryGetValue(queueKey, out var queueName)
            ? queueName
            : null;
    }

    private IntegrationRoutingRule? ResolveRoutingRule(IntegrationInboxEntry entry)
    {
        return processingOptions.RoutingRules.FirstOrDefault(rule =>
            rule.ClientId == entry.ClientId
            && rule.OriginTypes.Contains(entry.OriginType)
            && (string.IsNullOrWhiteSpace(rule.SourceSystemKey)
                || string.Equals(rule.SourceSystemKey, entry.SourceSystemKey, StringComparison.OrdinalIgnoreCase))
            && (rule.EntityTypes.Count == 0
                || rule.EntityTypes.Contains(entry.EntityType, StringComparer.OrdinalIgnoreCase))
            && (rule.EventTypes.Count == 0
                || rule.EventTypes.Contains(entry.EventType, StringComparer.OrdinalIgnoreCase)));
    }
}