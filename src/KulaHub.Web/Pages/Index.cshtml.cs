using KulaHub.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KulaHub.Web.Pages;

public sealed class IndexModel(IKulaHubCrmService crmService) : PageModel
{
    public IReadOnlyList<ClientLookupDto> Clients { get; private set; } = [];

    public IReadOnlyList<ContactListItemDto> Contacts { get; private set; } = [];

    public int? SelectedClientId { get; private set; }

    public async Task OnGetAsync(int? clientId, CancellationToken cancellationToken)
    {
        Clients = await crmService.GetClientsAsync(cancellationToken);
        SelectedClientId = clientId ?? Clients.FirstOrDefault()?.ClientId;

        if (SelectedClientId.HasValue)
        {
            Contacts = await crmService.GetContactsAsync(SelectedClientId.Value, cancellationToken);
        }
    }
}
