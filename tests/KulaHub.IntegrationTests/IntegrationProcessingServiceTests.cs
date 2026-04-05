using KulaHub.Data;
using KulaHub.Data.Entities;
using KulaHub.Functions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KulaHub.IntegrationTests;

public sealed class IntegrationProcessingServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly RecordingQueueMessageSender queueMessageSender = new();
    private KulaHubDbContext dbContext = null!;
    private IntegrationProcessingService processingService = null!;

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<KulaHubDbContext>()
            .UseSqlite(connection)
            .Options;

        dbContext = new KulaHubDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        processingService = new IntegrationProcessingService(
            dbContext,
            queueMessageSender,
            Options.Create(new ProcessingOptions
            {
                BatchSize = 50,
                RoutingRules =
                [
                    new()
                    {
                        ClientId = 4,
                        OriginTypes = [OriginType.ExternalClient],
                        Disposition = IntegrationDisposition.Outbound,
                        QueueKey = "Client4Outbound"
                    },
                    new()
                    {
                        ClientId = 3,
                        OriginTypes = [OriginType.InternalApp, OriginType.BatchJob],
                        Disposition = IntegrationDisposition.Inbound,
                        QueueKey = "Client3Inbound"
                    }
                ],
                QueueBindings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Client4Outbound"] = "clientid4-outbound",
                    ["Client3Inbound"] = "clientid3-inbound"
                }
            }),
            NullLogger<IntegrationProcessingService>.Instance);
    }

    public async Task DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task ProcessInboxAsync_RoutesMatchingEntriesAndMarksBatchProcessed()
    {
        dbContext.IntegrationInbox.AddRange(
            CreateInboxEntry(4, OriginType.ExternalClient, "Contact.Created", correlationId: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", traceParent: "00-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa-1111111111111111-01"),
            CreateInboxEntry(3, OriginType.InternalApp, "Form.Created", correlationId: "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", traceParent: "00-bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb-2222222222222222-01"),
            CreateInboxEntry(3, OriginType.ExternalClient, "Ignored.Event"));
        await dbContext.SaveChangesAsync();

        var processedCount = await processingService.ProcessInboxAsync(CancellationToken.None);

        var inboxEntries = await dbContext.IntegrationInbox.OrderBy(item => item.Id).ToListAsync();
        var dispatchEntries = await dbContext.IntegrationDispatch.OrderBy(item => item.Id).ToListAsync();
        var outboundEntry = dispatchEntries.Single(item => item.Disposition == IntegrationDisposition.Outbound);
        var inboundEntry = dispatchEntries.Single(item => item.Disposition == IntegrationDisposition.Inbound);

        Assert.Equal(3, processedCount);
        Assert.All(inboxEntries, item => Assert.NotNull(item.ProcessedUtc));
        Assert.Equal(4, outboundEntry.ClientId);
        Assert.Equal("Client4Outbound", outboundEntry.QueueKey);
        Assert.Equal(OriginType.ExternalClient, outboundEntry.OriginType);
        Assert.Equal("Contact.Created", outboundEntry.EventType);
        Assert.Equal("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", outboundEntry.CorrelationId);
        Assert.Equal("00-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa-1111111111111111-01", outboundEntry.TraceParent);
        Assert.Equal(3, inboundEntry.ClientId);
        Assert.Equal("Client3Inbound", inboundEntry.QueueKey);
        Assert.Equal(OriginType.InternalApp, inboundEntry.OriginType);
        Assert.Equal("Form.Created", inboundEntry.EventType);
        Assert.Equal("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", inboundEntry.CorrelationId);
        Assert.Equal("00-bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb-2222222222222222-01", inboundEntry.TraceParent);
    }

    [Fact]
    public async Task DispatchAsync_SendsQueueMessageAndMarksEntryDispatched()
    {
        var dispatchEntry = new IntegrationDispatchEntry
        {
            IntegrationInboxId = 100,
            CorrelationId = "11111111111111111111111111111111",
            TraceParent = "00-11111111111111111111111111111111-3333333333333333-01",
            ClientId = 4,
            Disposition = IntegrationDisposition.Outbound,
            OriginType = OriginType.ExternalClient,
            QueueKey = "Client4Outbound",
            EntityType = "Contact",
            EventType = "Contact.Created",
            ChangeType = "Created",
            ExternalEntityId = "123",
            PayloadJson = "{\"contactId\":123}",
            ReceivedUtc = DateTime.UtcNow
        };

        dbContext.IntegrationDispatch.Add(dispatchEntry);
        await dbContext.SaveChangesAsync();

        var dispatchedCount = await processingService.DispatchAsync(CancellationToken.None);
        var reloadedEntry = await dbContext.IntegrationDispatch.SingleAsync();

        Assert.Equal(1, dispatchedCount);
        Assert.Single(queueMessageSender.Messages);
        Assert.Equal("clientid4-outbound", queueMessageSender.Messages[0].QueueName);
        Assert.Equal(reloadedEntry.Id, queueMessageSender.Messages[0].Message.IntegrationEntryId);
        Assert.Equal("00-11111111111111111111111111111111-3333333333333333-01", queueMessageSender.Messages[0].TraceParent);
        Assert.Equal("11111111111111111111111111111111", queueMessageSender.Messages[0].CorrelationId);
        Assert.Equal("clientid4-outbound", reloadedEntry.DispatchTarget);
        Assert.NotNull(reloadedEntry.DispatchedUtc);
        Assert.Null(reloadedEntry.ProcessedUtc);
    }

    [Fact]
    public async Task DispatchAsync_IgnoresEntriesWithoutMatchingBinding()
    {
        dbContext.IntegrationDispatch.Add(new IntegrationDispatchEntry
        {
            IntegrationInboxId = 200,
            ClientId = 77,
            Disposition = IntegrationDisposition.Outbound,
            OriginType = OriginType.ExternalClient,
            QueueKey = "UnknownQueue",
            EntityType = "Contact",
            EventType = "Contact.Created",
            ChangeType = "Created",
            PayloadJson = "{}",
            ReceivedUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var dispatchedCount = await processingService.DispatchAsync(CancellationToken.None);
        var reloadedEntry = await dbContext.IntegrationDispatch.SingleAsync();

        Assert.Equal(1, dispatchedCount);
        Assert.Empty(queueMessageSender.Messages);
        Assert.Equal("ignored", reloadedEntry.DispatchTarget);
        Assert.NotNull(reloadedEntry.ProcessedUtc);
        Assert.Null(reloadedEntry.DispatchedUtc);
    }

    [Fact]
    public async Task ProcessInboxAsync_UsesFirstMatchingRuleOrder()
    {
        var orderedService = new IntegrationProcessingService(
            dbContext,
            queueMessageSender,
            Options.Create(new ProcessingOptions
            {
                BatchSize = 50,
                RoutingRules =
                [
                    new()
                    {
                        ClientId = 4,
                        OriginTypes = [OriginType.ExternalClient],
                        Disposition = IntegrationDisposition.Inbound,
                        QueueKey = "Client4Inbound"
                    },
                    new()
                    {
                        ClientId = 4,
                        OriginTypes = [OriginType.ExternalClient],
                        Disposition = IntegrationDisposition.Outbound,
                        QueueKey = "Client4Outbound"
                    }
                ],
                QueueBindings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Client4Inbound"] = "clientid4-inbound",
                    ["Client4Outbound"] = "clientid4-outbound"
                }
            }),
            NullLogger<IntegrationProcessingService>.Instance);

        dbContext.IntegrationInbox.Add(CreateInboxEntry(4, OriginType.ExternalClient, "Contact.Created"));
        await dbContext.SaveChangesAsync();

        await orderedService.ProcessInboxAsync(CancellationToken.None);

        var dispatchEntries = await dbContext.IntegrationDispatch.ToListAsync();
        Assert.Single(dispatchEntries);
        Assert.Equal(IntegrationDisposition.Inbound, dispatchEntries[0].Disposition);
        Assert.Equal(OriginType.ExternalClient, dispatchEntries[0].OriginType);
    }

    [Fact]
    public async Task ProcessInboxAsync_IgnoresEntriesWithoutMatchingRule()
    {
        dbContext.IntegrationInbox.Add(CreateInboxEntry(42, OriginType.System, "Contact.Created"));
        await dbContext.SaveChangesAsync();

        var processedCount = await processingService.ProcessInboxAsync(CancellationToken.None);

        Assert.Equal(1, processedCount);
        Assert.Empty(await dbContext.IntegrationDispatch.ToListAsync());

        var inboxEntry = await dbContext.IntegrationInbox.SingleAsync();
        Assert.NotNull(inboxEntry.ProcessedUtc);
    }

    [Fact]
    public async Task CompleteDispatchAsync_MarksEntriesProcessed()
    {
        var outboundEntry = new IntegrationDispatchEntry
        {
            IntegrationInboxId = 300,
            ClientId = 4,
            Disposition = IntegrationDisposition.Outbound,
            OriginType = OriginType.ExternalClient,
            QueueKey = "Client4Outbound",
            EntityType = "Contact",
            EventType = "Contact.Created",
            ChangeType = "Created",
            PayloadJson = "{}",
            ReceivedUtc = DateTime.UtcNow,
            DispatchedUtc = DateTime.UtcNow,
            DispatchTarget = "clientid4-outbound"
        };

        var inboundEntry = new IntegrationDispatchEntry
        {
            IntegrationInboxId = 301,
            ClientId = 3,
            Disposition = IntegrationDisposition.Inbound,
            OriginType = OriginType.InternalApp,
            QueueKey = "Client3Inbound",
            EntityType = "Form",
            EventType = "Form.Created",
            ChangeType = "Created",
            PayloadJson = "{}",
            ReceivedUtc = DateTime.UtcNow,
            DispatchedUtc = DateTime.UtcNow,
            DispatchTarget = "clientid3-inbound"
        };

        dbContext.IntegrationDispatch.AddRange(outboundEntry, inboundEntry);
        await dbContext.SaveChangesAsync();

        await processingService.CompleteDispatchAsync(outboundEntry.Id, CancellationToken.None);
        await processingService.CompleteDispatchAsync(inboundEntry.Id, CancellationToken.None);

        var reloadedEntries = await dbContext.IntegrationDispatch.OrderBy(item => item.Id).ToListAsync();
        var reloadedOutbound = reloadedEntries.Single(item => item.Disposition == IntegrationDisposition.Outbound);
        var reloadedInbound = reloadedEntries.Single(item => item.Disposition == IntegrationDisposition.Inbound);

        Assert.NotNull(reloadedOutbound.ProcessedUtc);
        Assert.NotNull(reloadedInbound.ProcessedUtc);
    }

    private static IntegrationInboxEntry CreateInboxEntry(int clientId, OriginType originType, string eventType, string? correlationId = null, string? traceParent = null)
    {
        return new IntegrationInboxEntry
        {
            CorrelationId = correlationId,
            TraceParent = traceParent,
            ClientId = clientId,
            OriginType = originType,
            SourceSystemKey = null,
            EntityType = eventType.StartsWith("Form", StringComparison.Ordinal) ? "Form" : "Contact",
            EventType = eventType,
            ChangeType = "Created",
            ExternalEntityId = Guid.NewGuid().ToString("N"),
            PayloadJson = "{}",
            ReceivedUtc = DateTime.UtcNow
        };
    }

    private sealed class RecordingQueueMessageSender : IQueueMessageSender
    {
        public List<(string QueueName, QueuedIntegrationMessage Message, string? TraceParent, string? CorrelationId)> Messages { get; } = [];

        public Task SendAsync(string queueName, QueuedIntegrationMessage message, string? traceParent, string? correlationId, CancellationToken cancellationToken)
        {
            Messages.Add((queueName, message, traceParent, correlationId));
            return Task.CompletedTask;
        }
    }
}