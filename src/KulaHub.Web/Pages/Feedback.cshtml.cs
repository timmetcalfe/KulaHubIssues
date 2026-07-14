using System.ComponentModel.DataAnnotations;
using KulaHub.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KulaHub.Web.Pages;

public sealed class FeedbackModel(IKulaHubCrmService crmService) : PageModel
{
    [BindProperty]
    public FeedbackInput Input { get; set; } = new();

    public bool Submitted { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await crmService.SubmitFeedbackAsync(
                new SubmitFeedbackCommand(Input.Name!, Input.Email!, Input.Comments!),
                cancellationToken);

            Submitted = true;
            return Page();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }

    public sealed class FeedbackInput
    {
        [Required]
        [Display(Name = "Name")]
        [MaxLength(100)]
        public string? Name { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email address")]
        [MaxLength(200)]
        public string? Email { get; set; }

        [Required]
        [Display(Name = "Comments")]
        public string? Comments { get; set; }
    }
}
