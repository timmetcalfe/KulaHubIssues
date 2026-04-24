using System.ComponentModel.DataAnnotations;
using KulaHub.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KulaHub.Web.Pages;

public sealed class FeedbackModel(IKulaHubCrmService crmService) : PageModel
{
    public bool Submitted { get; private set; }

    [BindProperty]
    public FeedbackInput Input { get; set; } = new();

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
                new SubmitFeedbackCommand(Input.Name!, Input.Email, Input.Comments!),
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
        [StringLength(100)]
        [Display(Name = "Name")]
        public string? Name { get; set; }

        [EmailAddress]
        [StringLength(60)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Required]
        [Display(Name = "Comments")]
        public string? Comments { get; set; }
    }
}
