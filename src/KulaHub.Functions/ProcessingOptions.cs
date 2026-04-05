namespace KulaHub.Functions;

public sealed class IntegrationRoutingRule
{
    public int ClientId { get; set; }
    public List<KulaHub.Data.OriginType> OriginTypes { get; set; } = [];
    public string? SourceSystemKey { get; set; }
    public List<string> EntityTypes { get; set; } = [];
    public List<string> EventTypes { get; set; } = [];
    public KulaHub.Data.IntegrationDisposition Disposition { get; set; }
    public string QueueKey { get; set; } = string.Empty;
}

public sealed class ProcessingOptions
{
    public const string SectionName = "ProcessingOptions";
    public const string DealerContactMirrorQueueSetting = "%ProcessingOptions:QueueBindings:DealerContactMirror%";
    public const string PolarisContactExportQueueSetting = "%ProcessingOptions:QueueBindings:PolarisContactExport%";
    public const string PolarisImportProcessingQueueSetting = "%ProcessingOptions:QueueBindings:PolarisImportProcessing%";

    public int BatchSize { get; set; } = 50;

    public List<IntegrationRoutingRule> RoutingRules { get; set; } =
    [
        // new()
        // {
        //     ClientId = 4,
        //     OriginTypes = [KulaHub.Data.OriginType.InternalApp],
        //     Disposition = KulaHub.Data.IntegrationDisposition.Inbound,
        //     QueueKey = "DealerContactMirror"
        // },
        // new()
        // {
        //     ClientId = 3,
        //     OriginTypes = [KulaHub.Data.OriginType.InternalApp],
        //     Disposition = KulaHub.Data.IntegrationDisposition.Outbound,
        //     QueueKey = "PolarisContactExport"
        // }
    ];

    public Dictionary<string, string> QueueBindings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}