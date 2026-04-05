using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using KulaHub.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KulaHub.Functions.Clients.Dealer;

public sealed class DealerContactMirrorService(
    KulaHubDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    ILogger<DealerContactMirrorService> logger)
{
    private const int DealerClientId = 4;
    private const int PolarisClientId = 3;
    private const string DealerSourceSystemKey = "Dealer";

    public async Task MirrorToPolarisIfRequiredAsync(QueuedIntegrationMessage payload, CancellationToken cancellationToken)
    {
        if (!ShouldMirror(payload))
        {
            return;
        }

        var contactPayload = JsonSerializer.Deserialize<MirroredContactPayload>(payload.PayloadJson)
            ?? throw new InvalidOperationException("The Dealer contact payload could not be deserialized.");

        if (await PolarisContactAlreadyExistsAsync(contactPayload, cancellationToken))
        {
            logger.LogInformation(
                "Skipped mirroring Dealer contact integration entry {IntegrationEntryId} because a matching Polaris contact already exists.",
                payload.IntegrationEntryId);
            return;
        }

        var httpClient = httpClientFactory.CreateClient("KulaHubApiClient");

        using var response = await httpClient.PostAsJsonAsync(
            $"api/clients/{PolarisClientId}/contacts",
            new CreateContactRequest(
                SourceContactId: contactPayload.ContactId,
                OrganisationId: null,
                FirstName: contactPayload.FirstName,
                LastName: contactPayload.LastName,
                Email: contactPayload.Email,
                Postcode: contactPayload.Postcode,
                SourceSystemKey: DealerSourceSystemKey,
                OriginType: OriginType.InternalApp),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        logger.LogInformation(
            "Mirrored Dealer contact integration entry {IntegrationEntryId} from client {ClientId} to Polaris client {TargetClientId}.",
            payload.IntegrationEntryId,
            payload.ClientId,
            PolarisClientId);
    }

    private async Task<bool> PolarisContactAlreadyExistsAsync(MirroredContactPayload contactPayload, CancellationToken cancellationToken)
    {
        return await dbContext.Contacts
            .AsNoTracking()
            .AnyAsync(
                contact => contact.ClientId == PolarisClientId
                    && contact.DeletedUtc == null
                    && contact.SourceContactId == contactPayload.ContactId,
                cancellationToken);
    }

    internal static bool ShouldMirror(QueuedIntegrationMessage payload)
    {
        return payload.ClientId == DealerClientId
            && string.Equals(payload.EntityType, "Contact", StringComparison.OrdinalIgnoreCase)
            && string.Equals(payload.ChangeType, "Created", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record CreateContactRequest(
        int? SourceContactId,
        int? OrganisationId,
        string? FirstName,
        string? LastName,
        string? Email,
        string? Postcode,
        string? SourceSystemKey,
        OriginType OriginType);

    private sealed record MirroredContactPayload(
        int ContactId,
        int ClientId,
        int? OrganisationId,
        string? FirstName,
        string? LastName,
        string? Email,
        string? Postcode,
        DateTime CreatedUtc,
        string CreatedBy,
        [property: JsonConverter(typeof(JsonStringEnumConverter))] OriginType OriginType);
}