using System.Diagnostics;
using System.Text.Json;
using KulaHub.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace KulaHub.Data;

public sealed class KulaHubCrmService(KulaHubDbContext dbContext) : IKulaHubCrmService
{
    public async Task<IReadOnlyList<ClientLookupDto>> GetClientsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Clients
            .AsNoTracking()
            .Where(client => client.DeletedUtc == null)
            .OrderBy(client => client.Name)
            .Select(client => new ClientLookupDto(client.ClientId, client.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LookupOptionDto>> GetOrganisationsAsync(int clientId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Organisations
            .AsNoTracking()
            .Where(organisation => organisation.ClientId == clientId && organisation.DeletedUtc == null)
            .OrderBy(organisation => organisation.Name)
            .Select(organisation => new LookupOptionDto(organisation.OrganisationId, organisation.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LookupOptionDto>> GetFormTypesAsync(int clientId, CancellationToken cancellationToken = default)
    {
        return await dbContext.FormTypes
            .AsNoTracking()
            .Where(formType => formType.ClientId == clientId && formType.DeletedUtc == null)
            .OrderBy(formType => formType.Name)
            .Select(formType => new LookupOptionDto(formType.FormTypeId, formType.Name ?? $"Form type {formType.FormTypeId}"))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ContactListItemDto>> GetContactsAsync(int clientId, CancellationToken cancellationToken = default)
    {
        var contacts = await (
            from contact in dbContext.Contacts.AsNoTracking()
            join organisation in dbContext.Organisations.AsNoTracking()
                on contact.OrganisationId equals organisation.OrganisationId into organisations
            from organisation in organisations.DefaultIfEmpty()
            where contact.ClientId == clientId && contact.DeletedUtc == null
            orderby contact.LastName, contact.FirstName, contact.ContactId
            select new
            {
                contact.ContactId,
                contact.FirstName,
                contact.LastName,
                contact.Email,
                OrganisationName = organisation != null ? organisation.Name : null,
                NoteCount = dbContext.Notes.Count(note => note.ContactId == contact.ContactId && note.DeletedUtc == null),
                FormCount = dbContext.Forms.Count(form => form.ContactId == contact.ContactId && form.DeletedUtc == null)
            })
            .ToListAsync(cancellationToken);

        return contacts
            .Select(contact => new ContactListItemDto(
                contact.ContactId,
                BuildFullName(contact.FirstName, contact.LastName),
                contact.Email,
                contact.OrganisationName,
                contact.NoteCount,
                contact.FormCount))
            .ToList();
    }

    public async Task<ContactDetailDto?> GetContactDetailAsync(int clientId, int contactId, CancellationToken cancellationToken = default)
    {
        var contact = await (
            from item in dbContext.Contacts.AsNoTracking()
            join organisation in dbContext.Organisations.AsNoTracking()
                on item.OrganisationId equals organisation.OrganisationId into organisations
            from organisation in organisations.DefaultIfEmpty()
            where item.ClientId == clientId && item.ContactId == contactId && item.DeletedUtc == null
            select new
            {
                item.ContactId,
                item.ClientId,
                item.FirstName,
                item.LastName,
                item.Email,
                item.Postcode,
                item.OrganisationId,
                OrganisationName = organisation != null ? organisation.Name : null
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (contact is null)
        {
            return null;
        }

        var notes = await dbContext.Notes
            .AsNoTracking()
            .Where(note => note.ClientId == clientId && note.ContactId == contactId && note.DeletedUtc == null)
            .OrderByDescending(note => note.CreatedUtc)
            .Select(note => new NoteDto(note.NoteId, note.Content, note.CreatedUtc, note.CreatedBy))
            .ToListAsync(cancellationToken);

        var forms = await (
            from form in dbContext.Forms.AsNoTracking()
            join formType in dbContext.FormTypes.AsNoTracking()
                on form.FormTypeId equals formType.FormTypeId into formTypes
            from formType in formTypes.DefaultIfEmpty()
            where form.ClientId == clientId && form.ContactId == contactId && form.DeletedUtc == null
            orderby form.CreatedUtc descending
            select new FormSummaryDto(
                form.FormId,
                formType != null ? formType.Name : null,
                form.Text1,
                form.Text2,
                form.Text3,
                form.DateTime1,
                form.DateTime2,
                form.CreatedUtc))
            .ToListAsync(cancellationToken);

        return new ContactDetailDto(
            contact.ContactId,
            contact.ClientId,
            BuildFullName(contact.FirstName, contact.LastName),
            contact.Email,
            contact.Postcode,
            contact.OrganisationId,
            contact.OrganisationName,
            notes,
            forms);
    }

    public async Task<CreateContactResult> CreateContactAsync(CreateContactCommand command, OriginType originType, CancellationToken cancellationToken = default)
    {
        await EnsureClientExistsAsync(command.ClientId, cancellationToken);

        if (command.OrganisationId.HasValue)
        {
            await EnsureOrganisationExistsAsync(command.ClientId, command.OrganisationId.Value, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(command.FirstName) &&
            string.IsNullOrWhiteSpace(command.LastName) &&
            string.IsNullOrWhiteSpace(command.Email))
        {
            throw new InvalidOperationException("A contact requires at least a first name, last name, or email address.");
        }

        var createdUtc = DateTime.UtcNow;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var contact = new Contact
        {
            ClientId = command.ClientId,
            SourceContactId = command.SourceContactId,
            OrganisationId = command.OrganisationId,
            FirstName = TrimToNull(command.FirstName),
            LastName = TrimToNull(command.LastName),
            Email = TrimToNull(command.Email),
            Postcode = TrimToNull(command.Postcode),
            CreatedUtc = createdUtc,
            CreatedBy = originType.ToString()
        };

        dbContext.Contacts.Add(contact);
        await dbContext.SaveChangesAsync(cancellationToken);

        AddInboxEntry(
            command.ClientId,
            entityType: "Contact",
            eventType: "Contact.Created",
            changeType: "Created",
            payload: new
            {
                contact.ContactId,
                contact.ClientId,
                contact.SourceContactId,
                contact.OrganisationId,
                contact.FirstName,
                contact.LastName,
                contact.Email,
                contact.Postcode,
                contact.CreatedUtc,
                contact.CreatedBy,
                SourceSystemKey = NormalizeSourceSystemKey(command.SourceSystemKey),
                OriginType = originType.ToString()
            },
            originType,
            receivedUtc: createdUtc,
            sourceSystemKey: command.SourceSystemKey);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new CreateContactResult(contact.ContactId);
    }

    public async Task<CreateNoteResult> AddNoteAsync(AddNoteCommand command, OriginType originType, CancellationToken cancellationToken = default)
    {
        await EnsureContactExistsAsync(command.ClientId, command.ContactId, cancellationToken);

        if (string.IsNullOrWhiteSpace(command.Content))
        {
            throw new InvalidOperationException("A note requires content.");
        }

        var createdUtc = DateTime.UtcNow;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var note = new Note
        {
            ClientId = command.ClientId,
            ContactId = command.ContactId,
            Content = command.Content.Trim(),
            CreatedUtc = createdUtc,
            CreatedBy = originType.ToString()
        };

        dbContext.Notes.Add(note);
        await dbContext.SaveChangesAsync(cancellationToken);

        AddInboxEntry(
            command.ClientId,
            entityType: "Note",
            eventType: "Note.Created",
            changeType: "Created",
            payload: new
            {
                note.NoteId,
                note.ClientId,
                note.ContactId,
                note.Content,
                note.CreatedUtc,
                note.CreatedBy,
                SourceSystemKey = NormalizeSourceSystemKey(command.SourceSystemKey),
                OriginType = originType.ToString()
            },
            originType,
            receivedUtc: createdUtc,
            sourceSystemKey: command.SourceSystemKey);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new CreateNoteResult(note.NoteId);
    }

    public async Task<CreateFormResult> CreateContactFormAsync(CreateContactFormCommand command, OriginType originType, CancellationToken cancellationToken = default)
    {
        await EnsureContactExistsAsync(command.ClientId, command.ContactId, cancellationToken);
        await EnsureFormTypeExistsAsync(command.ClientId, command.FormTypeId, cancellationToken);

        var createdUtc = DateTime.UtcNow;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var form = new Form
        {
            ClientId = command.ClientId,
            ContactId = command.ContactId,
            FormTypeId = command.FormTypeId,
            Text1 = TrimToNull(command.Text1),
            Text2 = TrimToNull(command.Text2),
            Text3 = TrimToNull(command.Text3),
            DateTime1 = command.DateTime1,
            DateTime2 = command.DateTime2,
            CreatedUtc = createdUtc,
            CreatedBy = originType.ToString()
        };

        dbContext.Forms.Add(form);
        await dbContext.SaveChangesAsync(cancellationToken);

        AddInboxEntry(
            command.ClientId,
            entityType: "Form",
            eventType: "Form.Created",
            changeType: "Created",
            payload: new
            {
                form.FormId,
                form.ClientId,
                form.ContactId,
                form.FormTypeId,
                form.Text1,
                form.Text2,
                form.Text3,
                form.DateTime1,
                form.DateTime2,
                form.CreatedUtc,
                form.CreatedBy,
                SourceSystemKey = NormalizeSourceSystemKey(command.SourceSystemKey),
                OriginType = originType.ToString()
            },
            originType,
            receivedUtc: createdUtc,
            sourceSystemKey: command.SourceSystemKey);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new CreateFormResult(form.FormId);
    }

    public async Task<SubmitFeedbackResult> SubmitFeedbackAsync(SubmitFeedbackCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new InvalidOperationException("A name is required to submit feedback.");
        }

        if (string.IsNullOrWhiteSpace(command.Comments))
        {
            throw new InvalidOperationException("Comments are required to submit feedback.");
        }

        var createdUtc = DateTime.UtcNow;

        var feedback = new Entities.Feedback
        {
            Name = command.Name.Trim(),
            Email = TrimToNull(command.Email),
            Comments = command.Comments.Trim(),
            CreatedUtc = createdUtc,
            CreatedBy = "WebFeedback"
        };

        dbContext.Feedbacks.Add(feedback);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new SubmitFeedbackResult(feedback.FeedbackId);
    }

    private void AddInboxEntry(int clientId, string entityType, string eventType, string changeType, object payload, OriginType originType, DateTime receivedUtc, string? sourceSystemKey = null)
    {
        var traceContext = GetTraceContext();
        var normalizedSourceSystemKey = NormalizeSourceSystemKey(sourceSystemKey);

        dbContext.IntegrationInbox.Add(new IntegrationInboxEntry
        {
            CorrelationId = traceContext.CorrelationId,
            TraceParent = traceContext.TraceParent,
            ClientId = clientId,
            OriginType = originType,
            SourceSystemKey = normalizedSourceSystemKey,
            EntityType = entityType,
            EventType = eventType,
            ChangeType = changeType,
            PayloadJson = JsonSerializer.Serialize(payload),
            ReceivedUtc = receivedUtc
        });
    }

    private static string? NormalizeSourceSystemKey(string? sourceSystemKey)
    {
        return string.IsNullOrWhiteSpace(sourceSystemKey)
            ? null
            : sourceSystemKey.Trim();
    }

    private static (string CorrelationId, string TraceParent) GetTraceContext()
    {
        if (Activity.Current is { } activity && activity.TraceId != default)
        {
            var correlationId = activity.TraceId.ToString();

            if (activity.IdFormat == ActivityIdFormat.W3C && !string.IsNullOrWhiteSpace(activity.Id))
            {
                return (correlationId, activity.Id);
            }

            var activitySpanId = activity.SpanId != default
                ? activity.SpanId
                : ActivitySpanId.CreateRandom();
            var traceFlags = activity.ActivityTraceFlags.HasFlag(ActivityTraceFlags.Recorded) ? "01" : "00";
            return (correlationId, $"00-{correlationId}-{activitySpanId}-{traceFlags}");
        }

        var traceId = ActivityTraceId.CreateRandom().ToString();
        var spanId = ActivitySpanId.CreateRandom();
        return (traceId, $"00-{traceId}-{spanId}-01");
    }

    private async Task EnsureClientExistsAsync(int clientId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Clients.AnyAsync(
            client => client.ClientId == clientId && client.DeletedUtc == null,
            cancellationToken);

        if (!exists)
        {
            throw new KeyNotFoundException($"Client {clientId} was not found.");
        }
    }

    private async Task EnsureOrganisationExistsAsync(int clientId, int organisationId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Organisations.AnyAsync(
            organisation => organisation.ClientId == clientId && organisation.OrganisationId == organisationId && organisation.DeletedUtc == null,
            cancellationToken);

        if (!exists)
        {
            throw new KeyNotFoundException($"Organisation {organisationId} was not found for client {clientId}.");
        }
    }

    private async Task EnsureContactExistsAsync(int clientId, int contactId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Contacts.AnyAsync(
            contact => contact.ClientId == clientId && contact.ContactId == contactId && contact.DeletedUtc == null,
            cancellationToken);

        if (!exists)
        {
            throw new KeyNotFoundException($"Contact {contactId} was not found for client {clientId}.");
        }
    }

    private async Task EnsureFormTypeExistsAsync(int clientId, int formTypeId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.FormTypes.AnyAsync(
            formType => formType.ClientId == clientId && formType.FormTypeId == formTypeId && formType.DeletedUtc == null,
            cancellationToken);

        if (!exists)
        {
            throw new KeyNotFoundException($"Form type {formTypeId} was not found for client {clientId}.");
        }
    }

    private static string BuildFullName(string? firstName, string? lastName)
    {
        var fullName = string.Join(" ", new[] { firstName?.Trim(), lastName?.Trim() }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return string.IsNullOrWhiteSpace(fullName) ? "Unnamed contact" : fullName;
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}