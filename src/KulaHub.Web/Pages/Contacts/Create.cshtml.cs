using System.ComponentModel.DataAnnotations;
using KulaHub.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KulaHub.Web.Pages.Contacts;

public sealed class CreateModel(IKulaHubCrmService crmService) : PageModel
{
    public int ClientId { get; private set; }

    public List<SelectListItem> OrganisationOptions { get; private set; } = [];

    [BindProperty]
    public CreateContactInput Input { get; set; } = new();

    public Task OnGetAsync(int clientId, CancellationToken cancellationToken)
    {
        ClientId = clientId;
        return LoadOptionsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(int clientId, CancellationToken cancellationToken)
    {
        ClientId = clientId;

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(cancellationToken);
            return Page();
        }

        try
        {
            var result = await crmService.CreateContactAsync(
                new CreateContactCommand(clientId, null, Input.OrganisationId, Input.FirstName, Input.LastName, Input.Email, Input.Postcode),
                OriginType.InternalApp,
                cancellationToken);

            return RedirectToPage("/Contacts/Details", new { clientId, contactId = result.ContactId });
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadOptionsAsync(cancellationToken);
            return Page();
        }
    }

    private async Task LoadOptionsAsync(CancellationToken cancellationToken)
    {
        var organisations = await crmService.GetOrganisationsAsync(ClientId, cancellationToken);
        OrganisationOptions = organisations
            .Select(organisation => new SelectListItem(organisation.Name, organisation.Id.ToString()))
            .ToList();
    }

    public sealed class CreateContactInput
    {
        [Display(Name = "Organisation")]
        public int? OrganisationId { get; set; }

        [StringLength(50)]
        [Display(Name = "First name")]
        public string? FirstName { get; set; }

        [StringLength(50)]
        [Display(Name = "Last name")]
        public string? LastName { get; set; }

        [EmailAddress]
        [StringLength(60)]
        public string? Email { get; set; }

        [StringLength(12)]
        public string? Postcode { get; set; }
    }
}