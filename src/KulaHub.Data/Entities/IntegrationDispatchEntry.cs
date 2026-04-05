namespace KulaHub.Data.Entities;

public sealed class IntegrationDispatchEntry
{
    public long Id { get; set; }
    public long IntegrationInboxId { get; set; }
    public string? CorrelationId { get; set; }
    public string? TraceParent { get; set; }
    public int ClientId { get; set; }
    public IntegrationDisposition Disposition { get; set; }
    public OriginType OriginType { get; set; }
    public string? SourceSystemKey { get; set; }
    public string QueueKey { get; set; } = string.Empty;
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