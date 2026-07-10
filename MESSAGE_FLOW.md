# Message Flow

This diagram shows the current end-to-end flow from an API write request through the inbox-processing pipeline and into the unified dispatch table and queue consumers.

```mermaid
flowchart TD
    A[Client calls API endpoint<br/>POST /api/clients/:clientId/contacts] --> B[ContactEndpoints.MapPost]
    B --> C[KulaHubCrmService.CreateContactAsync]
    C --> D[Insert Contact row<br/>save changes]
    D --> E[Insert IntegrationInbox row<br/>EventType = Contact.Created<br/>OriginType included in payload and row]
    E --> F[(IntegrationInbox table)]

    F --> G[ProcessIntegrationInbox timer function<br/>every minute at second 0]
    G --> H[IntegrationProcessingService.ProcessInboxAsync]
    H --> I{Routing rule match<br/>ClientId + OriginType<br/>+ optional SourceSystemKey}

    I -->|QueueKey matched| J[Create IntegrationDispatch row<br/>Disposition + QueueKey]
    I -->|No match| K[Mark IntegrationInbox row processed<br/>log ignored]

    J --> L[(IntegrationDispatch table)]

    L --> M[DispatchIntegrationDispatch timer function<br/>every minute at second 15]
    M --> N[IntegrationProcessingService.DispatchAsync]
    N --> O{Queue binding found?}
    O -->|Yes| P[Send Service Bus message<br/>to bound queue]
    O -->|No| Q[Mark dispatch row ignored]

    P --> R[DealerInternalConsumer<br/>or Polaris consumer trigger]
    R --> S[CompleteDispatchAsync<br/>mark IntegrationDispatch.ProcessedUtc]

    T[Current configured examples] --> U[ClientId 4 + InternalApp → DealerContactMirror]
    T --> V[ClientId 3 + InternalApp → PolarisContactExport]
    T --> W[ClientId 3 + ExternalClient → PolarisImportProcessing]
```

## Notes

- The API writes the business entity first and then writes an `IntegrationInbox` row in the same application workflow.
- `ProcessIntegrationInbox` is the routing step that decides whether an inbox item becomes a dispatch row with a `QueueKey`, or is ignored.
- `DispatchIntegrationDispatch` is the single timer-triggered dispatch stage for queued work.
- The Service Bus consumer functions only complete the relevant `IntegrationDispatch` row after a queue message is received and processed.
- The current routing model stores both a logical `QueueKey` and the resolved physical `DispatchTarget` queue name.

## Execution Order

This view focuses on when each function runs and the normal order you would enable and observe them locally.

```mermaid
sequenceDiagram
    autonumber
    participant API as API
    participant DB as Azure SQL
    participant InboxFn as ProcessIntegrationInbox<br/>(0 */1 * * * *)
    participant DispatchFn as DispatchIntegrationDispatch<br/>(15 */1 * * * *)
    participant SBOut as polaris-outbound
    participant SBIn as dealer
    participant Dealer as DealerInternalConsumer
    participant Polaris as PolarisOutboundConsumer

    API->>DB: Insert business row
    API->>DB: Insert IntegrationInbox row

    InboxFn->>DB: Read unprocessed IntegrationInbox batch
    InboxFn->>DB: Apply routing rules
    alt Route matched
        InboxFn->>DB: Insert IntegrationDispatch row
        InboxFn->>DB: Mark IntegrationInbox.ProcessedUtc
        DispatchFn->>DB: Read undispatched IntegrationDispatch batch
        DispatchFn->>SBOut: Send queue message when QueueKey resolves to PolarisContactExport
        DispatchFn->>SBIn: Send queue message when QueueKey resolves to DealerContactMirror
        DispatchFn->>DB: Set DispatchTarget and DispatchedUtc
        Polaris->>SBOut: Receive message
        Polaris->>DB: Mark IntegrationDispatch.ProcessedUtc
        Dealer->>SBIn: Receive message
        Dealer->>DB: Mark IntegrationDispatch.ProcessedUtc
    else No route matched
        InboxFn->>DB: Mark IntegrationInbox.ProcessedUtc only
    end
```

## Suggested Local Run Order

1. Run the API and submit a request.
2. Enable and run `ProcessIntegrationInbox` to watch routing occur.
3. Enable and run `DispatchIntegrationDispatch` to send queued work.
4. Enable and run the matching Service Bus consumer function to complete the integration row.

## Table State Transitions

This view focuses on how rows move through the integration tables.

```mermaid
stateDiagram-v2
    [*] --> BusinessWrite

    BusinessWrite: Business entity created
    BusinessWrite: Contact / Note / Form row saved
    BusinessWrite --> InboxPending: IntegrationInbox row inserted

    state "IntegrationInbox" as Inbox {
        InboxPending: Pending
        InboxProcessed: Processed
    }

    InboxPending --> DispatchPending: Routing rule matched
    InboxPending --> InboxProcessed: No rule matched

    state "IntegrationDispatch" as Dispatch {
        DispatchPending: Pending dispatch
        DispatchDispatched: Dispatched
        DispatchProcessed: Processed
        DispatchIgnored: Ignored
    }

    DispatchPending --> DispatchDispatched: DispatchIntegrationDispatch sets\nDispatchedUtc + DispatchTarget
    DispatchPending --> DispatchIgnored: No queue binding\nProcessedUtc set
    DispatchDispatched --> DispatchProcessed: Queue consumer sets\nProcessedUtc

    DispatchPending --> InboxProcessed: Inbox row marked processed
```

### Meaning Of The Columns

- `IntegrationInbox.ProcessedUtc`: set when the inbox event has been classified.
- `IntegrationDispatch.DispatchedUtc`: set when a queue message has been sent.
- `IntegrationDispatch.ProcessedUtc`: set when the queue consumer has completed handling the message.
- `QueueKey`: stores the logical routing decision used to resolve the queue binding.
- `DispatchTarget`: stores the queue name used for dispatch, or `ignored` if no queue binding matched.