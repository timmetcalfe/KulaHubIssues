namespace KulaHub.Data.Entities;

public sealed class Client
{
    public int ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Postcode { get; set; }
    public DateTime CreatedUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? DeletedUtc { get; set; }

    public ICollection<Organisation> Organisations { get; set; } = new List<Organisation>();
    public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
    public ICollection<Note> Notes { get; set; } = new List<Note>();
    public ICollection<FormType> FormTypes { get; set; } = new List<FormType>();
    public ICollection<Form> Forms { get; set; } = new List<Form>();
}