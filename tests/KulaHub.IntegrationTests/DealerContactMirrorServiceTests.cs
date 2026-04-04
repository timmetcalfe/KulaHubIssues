using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KulaHub.Data;
using KulaHub.Data.Entities;
using KulaHub.Functions;
using KulaHub.Functions.Clients.Dealer;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace KulaHub.IntegrationTests;

public sealed class DealerContactMirrorServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private DbContextOptions<KulaHubDbContext> dbContextOptions = null!;

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        dbContextOptions = new DbContextOptionsBuilder<KulaHubDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new KulaHubDbContext(dbContextOptions);
        await dbContext.Database.EnsureCreatedAsync();
        dbContext.Clients.AddRange(
            new Client
            {
                ClientId = 3,
                Name = "Polaris",
                CreatedUtc = DateTime.UtcNow,
                CreatedBy = "test"
            },
            new Client
            {
                ClientId = 4,
                Name = "Dealer",
                CreatedUtc = DateTime.UtcNow,
                CreatedBy = "test"
            });
        await dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task MirrorToPolarisIfRequiredAsync_PostsPolarisContactForDealerCreateMessage()
    {
        var handler = new RecordingHandler();
        await using var dbContext = CreateDbContext();
        var services = new ServiceCollection();
        services.AddHttpClient("KulaHubApiClient")
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .ConfigureHttpClient(client => client.BaseAddress = new Uri("http://localhost:5028/"));

        await using var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var service = new DealerContactMirrorService(dbContext, httpClientFactory, NullLogger<DealerContactMirrorService>.Instance);

        var payload = new QueuedIntegrationMessage(
            IntegrationEntryId: 42,
            ClientId: 4,
            EntityType: "Contact",
            EventType: "Contact.Created",
            ChangeType: "Created",
            PayloadJson: JsonSerializer.Serialize(new
            {
                ContactId = 99,
                ClientId = 4,
                OrganisationId = 77,
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane@example.com",
                Postcode = "AB12 3CD",
                CreatedUtc = DateTime.UtcNow,
                CreatedBy = "ExternalClient",
                OriginType = "ExternalClient"
            }),
            DispatchTarget: "dealer");

        await service.MirrorToPolarisIfRequiredAsync(payload, CancellationToken.None);

        Assert.NotNull(handler.Request);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("http://localhost:5028/api/clients/3/contacts", handler.Request.RequestUri!.ToString());

        var body = await handler.Request.Content!.ReadFromJsonAsync<CreateContactApiRequest>();
        Assert.NotNull(body);
        Assert.Equal(99, body!.SourceContactId);
        Assert.Null(body!.OrganisationId);
        Assert.Equal("Jane", body.FirstName);
        Assert.Equal("Doe", body.LastName);
        Assert.Equal("jane@example.com", body.Email);
        Assert.Equal("AB12 3CD", body.Postcode);
        Assert.Equal(OriginType.InternalApp, body.OriginType);
    }

    [Fact]
    public async Task MirrorToPolarisIfRequiredAsync_DoesNothingForNonDealerContactCreates()
    {
        var handler = new RecordingHandler();
        await using var dbContext = CreateDbContext();
        var services = new ServiceCollection();
        services.AddHttpClient("KulaHubApiClient")
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .ConfigureHttpClient(client => client.BaseAddress = new Uri("http://localhost:5028/"));

        await using var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var service = new DealerContactMirrorService(dbContext, httpClientFactory, NullLogger<DealerContactMirrorService>.Instance);

        var payload = new QueuedIntegrationMessage(
            IntegrationEntryId: 84,
            ClientId: 4,
            EntityType: "Note",
            EventType: "Note.Created",
            ChangeType: "Created",
            PayloadJson: "{}",
            DispatchTarget: "dealer");

        await service.MirrorToPolarisIfRequiredAsync(payload, CancellationToken.None);

        Assert.Null(handler.Request);
    }

    [Fact]
    public async Task MirrorToPolarisIfRequiredAsync_DoesNothingWhenMatchingPolarisContactAlreadyExists()
    {
        var handler = new RecordingHandler();
        await using var dbContext = CreateDbContext();
        dbContext.Contacts.Add(new Contact
        {
            ClientId = 3,
            SourceContactId = 99,
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com",
            Postcode = "AB12 3CD",
            CreatedUtc = DateTime.UtcNow,
            CreatedBy = "InternalApp"
        });
        await dbContext.SaveChangesAsync();

        var services = new ServiceCollection();
        services.AddHttpClient("KulaHubApiClient")
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .ConfigureHttpClient(client => client.BaseAddress = new Uri("http://localhost:5028/"));

        await using var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var service = new DealerContactMirrorService(dbContext, httpClientFactory, NullLogger<DealerContactMirrorService>.Instance);

        var payload = new QueuedIntegrationMessage(
            IntegrationEntryId: 85,
            ClientId: 4,
            EntityType: "Contact",
            EventType: "Contact.Created",
            ChangeType: "Created",
            PayloadJson: JsonSerializer.Serialize(new
            {
                ContactId = 99,
                ClientId = 4,
                OrganisationId = 77,
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane@example.com",
                Postcode = "AB12 3CD",
                CreatedUtc = DateTime.UtcNow,
                CreatedBy = "ExternalClient",
                OriginType = "ExternalClient"
            }),
            DispatchTarget: "dealer");

        await service.MirrorToPolarisIfRequiredAsync(payload, CancellationToken.None);

        Assert.Null(handler.Request);
    }

    private KulaHubDbContext CreateDbContext()
    {
        return new KulaHubDbContext(dbContextOptions);
    }

    private sealed record CreateContactApiRequest(
        int? SourceContactId,
        int? OrganisationId,
        string? FirstName,
        string? LastName,
        string? Email,
        string? Postcode,
        OriginType OriginType);

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created));
        }
    }
}