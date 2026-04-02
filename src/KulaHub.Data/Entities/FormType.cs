namespace KulaHub.Data.Entities;

public sealed class FormType
{
    public int FormTypeId { get; set; }
    public int ClientId { get; set; }
    public string? Name { get; set; }
    public DateTime CreatedUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? DeletedUtc { get; set; }

    public Client? Client { get; set; }
    public ICollection<Form> Forms { get; set; } = new List<Form>();
}