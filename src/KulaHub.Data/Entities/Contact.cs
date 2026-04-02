namespace KulaHub.Data.Entities;

public sealed class Contact
{
    public int ContactId { get; set; }
    public int ClientId { get; set; }
    public int? OrganisationId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Postcode { get; set; }
    public DateTime CreatedUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? DeletedUtc { get; set; }

    public Client? Client { get; set; }
    public Organisation? Organisation { get; set; }
    public ICollection<Note> Notes { get; set; } = new List<Note>();
    public ICollection<Form> Forms { get; set; } = new List<Form>();
}