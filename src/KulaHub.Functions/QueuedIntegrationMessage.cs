namespace KulaHub.Functions;

public sealed record QueuedIntegrationMessage(
    long IntegrationEntryId,
    int ClientId,
    string EntityType,
    string EventType,
    string ChangeType,
    string PayloadJson,
    string DispatchTarget);