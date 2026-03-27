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
                SouthbridgeOutboundClientId = 4,
                SouthbridgeOutboundQueueName = "clientid4-outbound",
                NorthwindInboundClientId = 3,
                NorthwindInboundQueueName = "clientid3-inbound"
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
            CreateInboxEntry(4, "Contact.Created"),
            CreateInboxEntry(3, "Form.Created"),
            CreateInboxEntry(99, "Ignored.Event"));
        await dbContext.SaveChangesAsync();

        var processedCount = await processingService.ProcessInboxAsync(CancellationToken.None);

        var inboxEntries = await dbContext.IntegrationInbox.OrderBy(item => item.Id).ToListAsync();
        var outboundEntry = await dbContext.IntegrationOutbound.SingleAsync();
        var inboundEntry = await dbContext.IntegrationInbound.SingleAsync();

        Assert.Equal(3, processedCount);
        Assert.All(inboxEntries, item => Assert.NotNull(item.ProcessedUtc));
        Assert.Equal(4, outboundEntry.ClientId);
        Assert.Equal("Contact.Created", outboundEntry.EventType);
        Assert.Equal(3, inboundEntry.ClientId);
        Assert.Equal("Form.Created", inboundEntry.EventType);
    }

    [Fact]
    public async Task DispatchOutboundAsync_SendsQueueMessageAndMarksEntryDispatched()
    {
        var outboundEntry = new IntegrationOutboundEntry
        {
            ClientId = 4,
            EntityType = "Contact",
            EventType = "Contact.Created",
            ChangeType = "Created",
            ExternalEntityId = "123",
            PayloadJson = "{\"contactId\":123}",
            ReceivedUtc = DateTime.UtcNow
        };

        dbContext.IntegrationOutbound.Add(outboundEntry);
        await dbContext.SaveChangesAsync();

        var dispatchedCount = await processingService.DispatchOutboundAsync(CancellationToken.None);
        var reloadedEntry = await dbContext.IntegrationOutbound.SingleAsync();

        Assert.Equal(1, dispatchedCount);
        Assert.Single(queueMessageSender.Messages);
        Assert.Equal("clientid4-outbound", queueMessageSender.Messages[0].QueueName);
        Assert.Equal(reloadedEntry.Id, queueMessageSender.Messages[0].Message.IntegrationEntryId);
        Assert.Equal("clientid4-outbound", reloadedEntry.DispatchTarget);
        Assert.NotNull(reloadedEntry.DispatchedUtc);
        Assert.Null(reloadedEntry.ProcessedUtc);
    }

    [Fact]
    public async Task DispatchInboundAsync_SendsQueueMessageAndMarksEntryDispatched()
    {
        var inboundEntry = new IntegrationInboundEntry
        {
            ClientId = 3,
            EntityType = "Form",
            EventType = "Form.Created",
            ChangeType = "Created",
            ExternalEntityId = "456",
            PayloadJson = "{\"formId\":456}",
            ReceivedUtc = DateTime.UtcNow
        };

        dbContext.IntegrationInbound.Add(inboundEntry);
        await dbContext.SaveChangesAsync();

        var dispatchedCount = await processingService.DispatchInboundAsync(CancellationToken.None);
        var reloadedEntry = await dbContext.IntegrationInbound.SingleAsync();

        Assert.Equal(1, dispatchedCount);
        Assert.Single(queueMessageSender.Messages);
        Assert.Equal("clientid3-inbound", queueMessageSender.Messages[0].QueueName);
        Assert.Equal(reloadedEntry.Id, queueMessageSender.Messages[0].Message.IntegrationEntryId);
        Assert.Equal("clientid3-inbound", reloadedEntry.DispatchTarget);
        Assert.NotNull(reloadedEntry.DispatchedUtc);
        Assert.Null(reloadedEntry.ProcessedUtc);
    }

    [Fact]
    public async Task DispatchOutboundAsync_IgnoresEntriesWithoutMatchingRule()
    {
        dbContext.IntegrationOutbound.Add(new IntegrationOutboundEntry
        {
            ClientId = 77,
            EntityType = "Contact",
            EventType = "Contact.Created",
            ChangeType = "Created",
            PayloadJson = "{}",
            ReceivedUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var dispatchedCount = await processingService.DispatchOutboundAsync(CancellationToken.None);
        var reloadedEntry = await dbContext.IntegrationOutbound.SingleAsync();

        Assert.Equal(1, dispatchedCount);
        Assert.Empty(queueMessageSender.Messages);
        Assert.Equal("ignored", reloadedEntry.DispatchTarget);
        Assert.NotNull(reloadedEntry.ProcessedUtc);
        Assert.Null(reloadedEntry.DispatchedUtc);
    }

    [Fact]
    public async Task CompleteMethods_MarkEntriesProcessed()
    {
        var outboundEntry = new IntegrationOutboundEntry
        {
            ClientId = 4,
            EntityType = "Contact",
            EventType = "Contact.Created",
            ChangeType = "Created",
            PayloadJson = "{}",
            ReceivedUtc = DateTime.UtcNow,
            DispatchedUtc = DateTime.UtcNow,
            DispatchTarget = "clientid4-outbound"
        };

        var inboundEntry = new IntegrationInboundEntry
        {
            ClientId = 3,
            EntityType = "Form",
            EventType = "Form.Created",
            ChangeType = "Created",
            PayloadJson = "{}",
            ReceivedUtc = DateTime.UtcNow,
            DispatchedUtc = DateTime.UtcNow,
            DispatchTarget = "clientid3-inbound"
        };

        dbContext.IntegrationOutbound.Add(outboundEntry);
        dbContext.IntegrationInbound.Add(inboundEntry);
        await dbContext.SaveChangesAsync();

        await processingService.CompleteOutboundAsync(outboundEntry.Id, CancellationToken.None);
        await processingService.CompleteInboundAsync(inboundEntry.Id, CancellationToken.None);

        var reloadedOutbound = await dbContext.IntegrationOutbound.SingleAsync();
        var reloadedInbound = await dbContext.IntegrationInbound.SingleAsync();

        Assert.NotNull(reloadedOutbound.ProcessedUtc);
        Assert.NotNull(reloadedInbound.ProcessedUtc);
    }

    private static IntegrationInboxEntry CreateInboxEntry(int clientId, string eventType)
    {
        return new IntegrationInboxEntry
        {
            ClientId = clientId,
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
        public List<(string QueueName, QueuedIntegrationMessage Message)> Messages { get; } = [];

        public Task SendAsync(string queueName, QueuedIntegrationMessage message, CancellationToken cancellationToken)
        {
            Messages.Add((queueName, message));
            return Task.CompletedTask;
        }
    }
}