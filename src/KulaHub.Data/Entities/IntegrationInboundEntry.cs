namespace KulaHub.Data.Entities;

public sealed class IntegrationInboundEntry
{
    public long Id { get; set; }
    public int ClientId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public string? ExternalEntityId { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime ReceivedUtc { get; set; }
    public DateTime? DispatchedUtc { get; set; }
    public string? DispatchTarget { get; set; }
    public DateTime? ProcessedUtc { get; set; }
}