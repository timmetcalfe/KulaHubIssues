namespace KulaHub.Functions;

public enum InboxRouteAction
{
    Ignore,
    Outbound,
    Inbound
}

public sealed class InboxRoutingRule
{
    public int ClientId { get; set; }
    public List<KulaHub.Data.OriginType> OriginTypes { get; set; } = [];
    public InboxRouteAction Action { get; set; }
}

public sealed class QueueRoutingRule
{
    public int ClientId { get; set; }
    public string QueueName { get; set; } = string.Empty;
}

// public sealed class ConsumerQueueBindingOptions
// {
//     // public string DealerInboundQueueName { get; set; } = "clientid4-inbound";

//     // public string PolarisOutboundQueueName { get; set; } = "polaris-outbound";

//     // public string PolarisInboundQueueName { get; set; } = "polaris-inbound";
// }

public sealed class ProcessingOptions
{
    public const string SectionName = "ProcessingOptions";
    public const string DealerInboundQueueSetting = "%ProcessingOptions:ConsumerQueueBindings:DealerInboundQueueName%";
    public const string PolarisOutboundQueueSetting = "%ProcessingOptions:ConsumerQueueBindings:PolarisOutboundQueueName%";
    public const string PolarisInboundQueueSetting = "%ProcessingOptions:ConsumerQueueBindings:PolarisInboundQueueName%";

    public int BatchSize { get; set; } = 50;

    //public ConsumerQueueBindingOptions ConsumerQueueBindings { get; set; } = new();

    public List<InboxRoutingRule> InboxRoutingRules { get; set; } =
    [
        // new()
        // {
        //     ClientId = 4,
        //     OriginTypes = [KulaHub.Data.OriginType.ExternalClient],
        //     Action = InboxRouteAction.Inbound
        // },
        // new()
        // {
        //     ClientId = 3,
        //     OriginTypes =
        //     [
        //         KulaHub.Data.OriginType.InternalApp,
        //         KulaHub.Data.OriginType.BackOfficeUser,
        //         KulaHub.Data.OriginType.BatchJob,
        //         KulaHub.Data.OriginType.System
        //     ],
        //     Action = InboxRouteAction.Inbound
        // }
    ];

    public List<QueueRoutingRule> OutboundQueueRules { get; set; } =
    [
        // new()
        // {
        //     ClientId = 3,
        //     QueueName = "clientid3-outbound"
        // }
    ];

    public List<QueueRoutingRule> InboundQueueRules { get; set; } =
    [
        // new()
        // {
        //     ClientId = 4,
        //     QueueName = "clientid4-inbound"
        // }
    ];
}