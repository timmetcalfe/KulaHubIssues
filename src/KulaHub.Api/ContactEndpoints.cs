using System.ComponentModel.DataAnnotations;
using KulaHub.Data;

namespace KulaHub.Api;

public static class ContactEndpoints
{
    public static IEndpointRouteBuilder MapContactEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/clients");

        group.MapGet("/", async (IKulaHubCrmService crmService, CancellationToken cancellationToken) =>
        {
            var clients = await crmService.GetClientsAsync(cancellationToken);
            return Results.Ok(clients);
        });

        group.MapGet("/{clientId:int}/organisations", async (int clientId, IKulaHubCrmService crmService, CancellationToken cancellationToken) =>
        {
            var organisations = await crmService.GetOrganisationsAsync(clientId, cancellationToken);
            return Results.Ok(organisations);
        });

        group.MapGet("/{clientId:int}/form-types", async (int clientId, IKulaHubCrmService crmService, CancellationToken cancellationToken) =>
        {
            var formTypes = await crmService.GetFormTypesAsync(clientId, cancellationToken);
            return Results.Ok(formTypes);
        });

        group.MapGet("/{clientId:int}/contacts", async (int clientId, IKulaHubCrmService crmService, CancellationToken cancellationToken) =>
        {
            var contacts = await crmService.GetContactsAsync(clientId, cancellationToken);
            return Results.Ok(contacts);
        });

        group.MapGet("/{clientId:int}/contacts/{contactId:int}", async (int clientId, int contactId, IKulaHubCrmService crmService, CancellationToken cancellationToken) =>
        {
            var contact = await crmService.GetContactDetailAsync(clientId, contactId, cancellationToken);
            return contact is null ? Results.NotFound() : Results.Ok(contact);
        });

        group.MapPost("/{clientId:int}/contacts", async (int clientId, CreateContactBody body, IKulaHubCrmService crmService, CancellationToken cancellationToken) =>
        {
            var validationProblem = Validate(body);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            try
            {
                var result = await crmService.CreateContactAsync(
                    new CreateContactCommand(clientId, body.OrganisationId, body.FirstName, body.LastName, body.Email, body.Postcode),
                    actor: "web-api",
                    cancellationToken);

                return Results.Created($"/api/clients/{clientId}/contacts/{result.ContactId}", result);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        group.MapPost("/{clientId:int}/contacts/{contactId:int}/notes", async (int clientId, int contactId, CreateNoteBody body, IKulaHubCrmService crmService, CancellationToken cancellationToken) =>
        {
            var validationProblem = Validate(body);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            try
            {
                var result = await crmService.AddNoteAsync(
                    new AddNoteCommand(clientId, contactId, body.Content),
                    actor: "web-api",
                    cancellationToken);

                return Results.Created($"/api/clients/{clientId}/contacts/{contactId}", result);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        group.MapPost("/{clientId:int}/contacts/{contactId:int}/forms", async (int clientId, int contactId, CreateFormBody body, IKulaHubCrmService crmService, CancellationToken cancellationToken) =>
        {
            var validationProblem = Validate(body);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            try
            {
                var result = await crmService.CreateContactFormAsync(
                    new CreateContactFormCommand(clientId, contactId, body.FormTypeId, body.Text1, body.Text2, body.Text3, body.DateTime1, body.DateTime2),
                    actor: "web-api",
                    cancellationToken);

                return Results.Created($"/api/clients/{clientId}/contacts/{contactId}", result);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        return endpoints;
    }

    private static IResult? Validate<T>(T body)
    {
        var validationContext = new ValidationContext(body!);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(body!, validationContext, validationResults, validateAllProperties: true);

        if (isValid)
        {
            return null;
        }

        var errors = validationResults
            .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty), (result, member) => new { member, result.ErrorMessage })
            .GroupBy(item => string.IsNullOrWhiteSpace(item.member) ? "body" : item.member)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.ErrorMessage ?? "Invalid value.").ToArray());

        return Results.ValidationProblem(errors);
    }

    public sealed record CreateContactBody(
        int? OrganisationId,
        string? FirstName,
        string? LastName,
        [property: EmailAddress] string? Email,
        string? Postcode);

    public sealed record CreateNoteBody([property: Required] string Content);

    public sealed record CreateFormBody(
        [property: Range(1, int.MaxValue)] int FormTypeId,
        string? Text1,
        string? Text2,
        string? Text3,
        DateTime? DateTime1,
        DateTime? DateTime2);
}