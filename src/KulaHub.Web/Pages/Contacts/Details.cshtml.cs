using System.ComponentModel.DataAnnotations;
using KulaHub.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KulaHub.Web.Pages.Contacts;

public sealed class DetailsModel(IKulaHubCrmService crmService) : PageModel
{
    public int ClientId { get; private set; }

    public ContactDetailDto? Contact { get; private set; }

    public List<SelectListItem> FormTypeOptions { get; private set; } = [];

    [BindProperty]
    public AddNoteInput NoteInput { get; set; } = new();

    [BindProperty]
    public AddFormInput FormInput { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int clientId, int contactId, CancellationToken cancellationToken)
    {
        ClientId = clientId;
        await LoadPageAsync(clientId, contactId, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAddNoteAsync(int clientId, int contactId, CancellationToken cancellationToken)
    {
        ClientId = clientId;

        if (!TryValidateModel(NoteInput, nameof(NoteInput)))
        {
            await LoadPageAsync(clientId, contactId, cancellationToken);
            return Page();
        }

        try
        {
            await crmService.AddNoteAsync(
                new AddNoteCommand(clientId, contactId, NoteInput.Content),
                OriginType.InternalApp,
                cancellationToken);

            return RedirectToPage(new { clientId, contactId });
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadPageAsync(clientId, contactId, cancellationToken);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAddFormAsync(int clientId, int contactId, CancellationToken cancellationToken)
    {
        ClientId = clientId;

        if (!TryValidateModel(FormInput, nameof(FormInput)))
        {
            await LoadPageAsync(clientId, contactId, cancellationToken);
            return Page();
        }

        try
        {
            await crmService.CreateContactFormAsync(
                new CreateContactFormCommand(clientId, contactId, FormInput.FormTypeId, FormInput.Text1, FormInput.Text2, FormInput.Text3, FormInput.DateTime1, FormInput.DateTime2),
                OriginType.InternalApp,
                cancellationToken);

            return RedirectToPage(new { clientId, contactId });
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadPageAsync(clientId, contactId, cancellationToken);
            return Page();
        }
    }

    private async Task LoadPageAsync(int clientId, int contactId, CancellationToken cancellationToken)
    {
        Contact = await crmService.GetContactDetailAsync(clientId, contactId, cancellationToken);

        var formTypes = await crmService.GetFormTypesAsync(clientId, cancellationToken);
        FormTypeOptions = formTypes
            .Select(formType => new SelectListItem(formType.Name, formType.Id.ToString()))
            .ToList();

        if (FormTypeOptions.Count > 0 && FormInput.FormTypeId == 0)
        {
            FormInput.FormTypeId = int.Parse(FormTypeOptions[0].Value!, System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    public sealed class AddNoteInput
    {
        [Required]
        [Display(Name = "Note")]
        public string Content { get; set; } = string.Empty;
    }

    public sealed class AddFormInput
    {
        [Range(1, int.MaxValue)]
        [Display(Name = "Form type")]
        public int FormTypeId { get; set; }

        [Display(Name = "Text 1")]
        public string? Text1 { get; set; }

        [Display(Name = "Text 2")]
        public string? Text2 { get; set; }

        [Display(Name = "Text 3")]
        public string? Text3 { get; set; }

        [Display(Name = "Date 1")]
        [DataType(DataType.DateTime)]
        public DateTime? DateTime1 { get; set; }

        [Display(Name = "Date 2")]
        [DataType(DataType.DateTime)]
        public DateTime? DateTime2 { get; set; }
    }
}