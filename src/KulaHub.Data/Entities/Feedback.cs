namespace KulaHub.Data.Entities;

public sealed class Feedback
{
    public int FeedbackId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? DeletedUtc { get; set; }
}
