using System.Diagnostics;
using KulaHub.Data;
using KulaHub.Data.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KulaHub.IntegrationTests;

public sealed class KulaHubCrmServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private KulaHubDbContext dbContext = null!;
    private IKulaHubCrmService crmService = null!;

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<KulaHubDbContext>()
            .UseSqlite(connection)
            .Options;

        dbContext = new KulaHubDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        dbContext.Clients.Add(new Client
        {
            ClientId = 4,
            Name = "Dealer",
            CreatedUtc = DateTime.UtcNow,
            CreatedBy = "test"
        });

        dbContext.FormTypes.Add(new FormType
        {
            FormTypeId = 100,
            ClientId = 4,
            Name = "Sales form",
            CreatedUtc = DateTime.UtcNow,
            CreatedBy = "test"
        });

        await dbContext.SaveChangesAsync();

        crmService = new KulaHubCrmService(dbContext);
    }

    public async Task DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task CreateContactAsync_PersistsContactAndInboxEntry()
    {
        using var activity = new Activity("create-contact-request");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        var result = await crmService.CreateContactAsync(
            new CreateContactCommand(4, 4001, null, "Ava", "Stone", "ava.stone@dealer.example", "SR2 5CC", "Dealer"),
            OriginType.ExternalClient);

        var contact = await dbContext.Contacts.SingleAsync(item => item.ContactId == result.ContactId);
        var inboxEntry = await dbContext.IntegrationInbox.SingleAsync();

        Assert.Equal(4, contact.ClientId);
        Assert.Equal(4001, contact.SourceContactId);
        Assert.Equal("Ava", contact.FirstName);
        Assert.Equal(nameof(OriginType.ExternalClient), contact.CreatedBy);
        Assert.Equal(OriginType.ExternalClient, inboxEntry.OriginType);
        Assert.Equal("Dealer", inboxEntry.SourceSystemKey);
        Assert.Equal("Contact", inboxEntry.EntityType);
        Assert.Equal("Contact.Created", inboxEntry.EventType);
        Assert.Equal(activity.TraceId.ToString(), inboxEntry.CorrelationId);
        Assert.Equal(activity.Id, inboxEntry.TraceParent);
    }

    [Fact]
    public async Task AddNoteAsync_PersistsNoteAndInboxEntry()
    {
        var contact = await crmService.CreateContactAsync(
            new CreateContactCommand(4, null, null, "Noah", "Foster", "noah.foster@dealer.example", null),
            OriginType.InternalApp);

        var noteResult = await crmService.AddNoteAsync(
            new AddNoteCommand(4, contact.ContactId, "Confirmed follow-up workshop."),
            OriginType.InternalApp);

        var note = await dbContext.Notes.SingleAsync(item => item.NoteId == noteResult.NoteId);
        var inboxEntries = await dbContext.IntegrationInbox.OrderBy(item => item.Id).ToListAsync();

        Assert.Equal(contact.ContactId, note.ContactId);
        Assert.Equal(nameof(OriginType.InternalApp), note.CreatedBy);
        Assert.Equal(2, inboxEntries.Count);
        Assert.Equal(OriginType.InternalApp, inboxEntries[0].OriginType);
        Assert.Equal(OriginType.InternalApp, inboxEntries[1].OriginType);
        Assert.Equal("Note", inboxEntries[1].EntityType);
        Assert.Equal("Note.Created", inboxEntries[1].EventType);
    }

    [Fact]
    public async Task SubmitFeedbackAsync_PersistsFeedbackRow()
    {
        var result = await crmService.SubmitFeedbackAsync(
            new SubmitFeedbackCommand("Jane Smith", "jane.smith@example.com", "Great product!"));

        var feedback = await dbContext.Feedback.SingleAsync(item => item.FeedbackId == result.FeedbackId);

        Assert.Equal("Jane Smith", feedback.Name);
        Assert.Equal("jane.smith@example.com", feedback.Email);
        Assert.Equal("Great product!", feedback.Comments);
        Assert.Equal("FeedbackForm", feedback.CreatedBy);
    }

    [Fact]
    public async Task SubmitFeedbackAsync_ThrowsWhenNameIsMissing()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            crmService.SubmitFeedbackAsync(
                new SubmitFeedbackCommand("", "jane@example.com", "Comments")));
    }

    [Fact]
    public async Task SubmitFeedbackAsync_ThrowsWhenEmailIsMissing()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            crmService.SubmitFeedbackAsync(
                new SubmitFeedbackCommand("Jane", "", "Comments")));
    }

    [Fact]
    public async Task SubmitFeedbackAsync_ThrowsWhenCommentsAreMissing()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            crmService.SubmitFeedbackAsync(
                new SubmitFeedbackCommand("Jane", "jane@example.com", "")));
    }
}