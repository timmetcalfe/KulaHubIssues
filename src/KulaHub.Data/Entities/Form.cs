namespace KulaHub.Data.Entities;

public sealed class Form
{
    public int FormId { get; set; }
    public int ClientId { get; set; }
    public int FormTypeId { get; set; }
    public int? OrganisationId { get; set; }
    public int? ContactId { get; set; }
    public string? Text1 { get; set; }
    public string? Text2 { get; set; }
    public string? Text3 { get; set; }
    public DateTime? DateTime1 { get; set; }
    public DateTime? DateTime2 { get; set; }
    public DateTime CreatedUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? DeletedUtc { get; set; }
    public int? OriginalFormId { get; set; }

    public Client? Client { get; set; }
    public FormType? FormType { get; set; }
    public Organisation? Organisation { get; set; }
    public Contact? Contact { get; set; }
}