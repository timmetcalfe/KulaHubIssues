using System.Text.Json.Serialization;

namespace KulaHub.Data;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OriginType
{
    ExternalClient,
    InternalApp,
    BackOfficeUser,
    BatchJob,
    System
}

public sealed record ClientLookupDto(int ClientId, string Name);

public sealed record LookupOptionDto(int Id, string Name);

public sealed record ContactListItemDto(
    int ContactId,
    string FullName,
    string? Email,
    string? OrganisationName,
    int NoteCount,
    int FormCount);

public sealed record NoteDto(int NoteId, string Content, DateTime CreatedUtc, string CreatedBy);

public sealed record FormSummaryDto(
    int FormId,
    string? FormTypeName,
    string? Text1,
    string? Text2,
    string? Text3,
    DateTime? DateTime1,
    DateTime? DateTime2,
    DateTime CreatedUtc);

public sealed record ContactDetailDto(
    int ContactId,
    int ClientId,
    string FullName,
    string? Email,
    string? Postcode,
    int? OrganisationId,
    string? OrganisationName,
    IReadOnlyList<NoteDto> Notes,
    IReadOnlyList<FormSummaryDto> Forms);

public sealed record CreateContactCommand(
    int ClientId,
    int? SourceContactId,
    int? OrganisationId,
    string? FirstName,
    string? LastName,
    string? Email,
    string? Postcode);

public sealed record AddNoteCommand(int ClientId, int ContactId, string Content);

public sealed record CreateContactFormCommand(
    int ClientId,
    int ContactId,
    int FormTypeId,
    string? Text1,
    string? Text2,
    string? Text3,
    DateTime? DateTime1,
    DateTime? DateTime2);

public sealed record CreateContactResult(int ContactId);

public sealed record CreateNoteResult(int NoteId);

public sealed record CreateFormResult(int FormId);

public interface IKulaHubCrmService
{
    Task<IReadOnlyList<ClientLookupDto>> GetClientsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupOptionDto>> GetOrganisationsAsync(int clientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupOptionDto>> GetFormTypesAsync(int clientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContactListItemDto>> GetContactsAsync(int clientId, CancellationToken cancellationToken = default);
    Task<ContactDetailDto?> GetContactDetailAsync(int clientId, int contactId, CancellationToken cancellationToken = default);
    Task<CreateContactResult> CreateContactAsync(CreateContactCommand command, OriginType originType, CancellationToken cancellationToken = default);
    Task<CreateNoteResult> AddNoteAsync(AddNoteCommand command, OriginType originType, CancellationToken cancellationToken = default);
    Task<CreateFormResult> CreateContactFormAsync(CreateContactFormCommand command, OriginType originType, CancellationToken cancellationToken = default);
}