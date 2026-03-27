namespace KulaHub.Data.Entities;

public sealed class Organisation
{
    public int OrganisationId { get; set; }
    public int ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Postcode { get; set; }
    public DateTime CreatedUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? DeletedUtc { get; set; }

    public Client? Client { get; set; }
    public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
    public ICollection<Form> Forms { get; set; } = new List<Form>();
}