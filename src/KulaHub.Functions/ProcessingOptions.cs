namespace KulaHub.Functions;

public sealed class ProcessingOptions
{
    public const string SectionName = "ProcessingOptions";

    public int BatchSize { get; set; } = 50;
    public int SouthbridgeOutboundClientId { get; set; } = 4;
    public string SouthbridgeOutboundQueueName { get; set; } = "clientid4-outbound";
    public int NorthwindInboundClientId { get; set; } = 3;
    public string NorthwindInboundQueueName { get; set; } = "clientid3-inbound";
}