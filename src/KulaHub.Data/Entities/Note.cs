namespace KulaHub.Data.Entities;

public sealed class Note
{
    public int NoteId { get; set; }
    public int ClientId { get; set; }
    public int ContactId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? DeletedUtc { get; set; }

    public Client? Client { get; set; }
    public Contact? Contact { get; set; }
}