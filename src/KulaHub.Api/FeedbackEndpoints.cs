using System.ComponentModel.DataAnnotations;
using KulaHub.Data;

namespace KulaHub.Api;

public static class FeedbackEndpoints
{
    public static IEndpointRouteBuilder MapFeedbackEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/feedback", async (SubmitFeedbackBody body, IKulaHubCrmService crmService, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await crmService.SubmitFeedbackAsync(
                    new SubmitFeedbackCommand(body.Name ?? string.Empty, body.Email, body.Comments ?? string.Empty),
                    cancellationToken);

                return Results.Created($"/api/feedback/{result.FeedbackId}", result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        return endpoints;
    }

    public sealed record SubmitFeedbackBody(
        [property: Required] string? Name,
        [property: EmailAddress] string? Email,
        [property: Required] string? Comments);
}
