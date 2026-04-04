# Message Flow

This diagram shows the current end-to-end flow from an API write request through the inbox-processing pipeline and into the outbound or inbound queue consumers.

```mermaid
flowchart TD
    A[Client calls API endpoint<br/>POST /api/clients/:clientId/contacts] --> B[ContactEndpoints.MapPost]
    B --> C[KulaHubCrmService.CreateContactAsync]
    C --> D[Insert Contact row<br/>save changes]
    D --> E[Insert IntegrationInbox row<br/>EventType = Contact.Created<br/>OriginType included in payload and row]
    E --> F[(IntegrationInbox table)]

    F --> G[ProcessIntegrationInbox timer function<br/>every minute at second 0]
    G --> H[IntegrationProcessingService.ProcessInboxAsync]
    H --> I{Routing rule match<br/>ClientId + OriginType}

    I -->|Outbound rule| J[Create IntegrationOutbound row]
    I -->|Inbound rule| K[Create IntegrationInbound row]
    I -->|No match| L[Mark IntegrationInbox row processed<br/>log ignored]

    J --> M[(IntegrationOutbound table)]
    K --> N[(IntegrationInbound table)]

    M --> O[DispatchIntegrationOutbound timer function<br/>every minute at second 15]
    O --> P[IntegrationProcessingService.DispatchOutboundAsync]
    P --> Q{Outbound queue rule found?}
    Q -->|Yes| R[Send Service Bus message<br/>to clientid3-outbound]
    Q -->|No| S[Mark outbound row ignored]

    N --> T[DispatchIntegrationInbound timer function<br/>every minute at second 30]
    T --> U[IntegrationProcessingService.DispatchInboundAsync]
    U --> V{Inbound queue rule found?}
    V -->|Yes| W[Send Service Bus message<br/>to clientid4-inbound]
    V -->|No| X[Mark inbound row ignored]

    W --> Y[DealerInboundConsumer<br/>Service Bus trigger]
    Y --> Z[CompleteInboundAsync<br/>mark IntegrationInbound.ProcessedUtc]

    R --> AA[PolarisOutboundConsumer<br/>Service Bus trigger]
    AA --> AB[CompleteOutboundAsync<br/>mark IntegrationOutbound.ProcessedUtc]

    AC[Current configured examples] --> AD[ClientId 4 + ExternalClient -> inbound]
    AC --> AE[ClientId 3 + InternalApp/BackOfficeUser/BatchJob/System -> outbound]
```

## Notes

- The API writes the business entity first and then writes an `IntegrationInbox` row in the same application workflow.
- `ProcessIntegrationInbox` is the routing step that decides whether an inbox item becomes an outbound message, an inbound message, or is ignored.
- `DispatchIntegrationOutbound` and `DispatchIntegrationInbound` are separate timer-triggered dispatch stages.
- The Service Bus consumer functions only complete the relevant integration row after a queue message is received and processed.
- The current queue names are `clientid4-inbound` for the Dealer inbound path and `clientid3-outbound` for the Polaris outbound path.

## Execution Order

This view focuses on when each function runs and the normal order you would enable and observe them locally.

```mermaid
sequenceDiagram
    autonumber
    participant API as API
    participant DB as Azure SQL
    participant InboxFn as ProcessIntegrationInbox<br/>(0 */1 * * * *)
    participant OutFn as DispatchIntegrationOutbound<br/>(15 */1 * * * *)
    participant InFn as DispatchIntegrationInbound<br/>(30 */1 * * * *)
    participant SBOut as clientid3-outbound
    participant SBIn as clientid4-inbound
    participant Dealer as DealerInboundConsumer
    participant Polaris as PolarisOutboundConsumer

    API->>DB: Insert business row
    API->>DB: Insert IntegrationInbox row

    InboxFn->>DB: Read unprocessed IntegrationInbox batch
    InboxFn->>DB: Apply routing rules
    alt Outbound route matched
        InboxFn->>DB: Insert IntegrationOutbound row
        InboxFn->>DB: Mark IntegrationInbox.ProcessedUtc
        OutFn->>DB: Read undispatched IntegrationOutbound batch
        OutFn->>SBOut: Send queue message
        OutFn->>DB: Set DispatchTarget and DispatchedUtc
        Polaris->>SBOut: Receive message
        Polaris->>DB: Mark IntegrationOutbound.ProcessedUtc
    else Inbound route matched
        InboxFn->>DB: Insert IntegrationInbound row
        InboxFn->>DB: Mark IntegrationInbox.ProcessedUtc
        InFn->>DB: Read undispatched IntegrationInbound batch
        InFn->>SBIn: Send queue message
        InFn->>DB: Set DispatchTarget and DispatchedUtc
        Dealer->>SBIn: Receive message
        Dealer->>DB: Mark IntegrationInbound.ProcessedUtc
    else No route matched
        InboxFn->>DB: Mark IntegrationInbox.ProcessedUtc only
    end
```

## Suggested Local Run Order

1. Run the API and submit a request.
2. Enable and run `ProcessIntegrationInbox` to watch routing occur.
3. Enable and run `DispatchIntegrationOutbound` or `DispatchIntegrationInbound` depending on the expected route.
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

    InboxPending --> OutboundPending: Outbound rule matched
    InboxPending --> InboundPending: Inbound rule matched
    InboxPending --> InboxProcessed: No rule matched

    state "IntegrationOutbound" as Outbound {
        OutboundPending: Pending dispatch
        OutboundDispatched: Dispatched
        OutboundProcessed: Processed
        OutboundIgnored: Ignored
    }

    state "IntegrationInbound" as Inbound {
        InboundPending: Pending dispatch
        InboundDispatched: Dispatched
        InboundProcessed: Processed
        InboundIgnored: Ignored
    }

    OutboundPending --> OutboundDispatched: DispatchIntegrationOutbound sets\nDispatchedUtc + DispatchTarget
    OutboundPending --> OutboundIgnored: No outbound queue rule\nProcessedUtc set
    OutboundDispatched --> OutboundProcessed: PolarisOutboundConsumer sets\nProcessedUtc

    InboundPending --> InboundDispatched: DispatchIntegrationInbound sets\nDispatchedUtc + DispatchTarget
    InboundPending --> InboundIgnored: No inbound queue rule\nProcessedUtc set
    InboundDispatched --> InboundProcessed: DealerInboundConsumer sets\nProcessedUtc

    OutboundPending --> InboxProcessed: Inbox row marked processed
    InboundPending --> InboxProcessed: Inbox row marked processed
```

### Meaning Of The Columns

- `IntegrationInbox.ProcessedUtc`: set when the inbox event has been classified.
- `IntegrationOutbound.DispatchedUtc` / `IntegrationInbound.DispatchedUtc`: set when a queue message has been sent.
- `IntegrationOutbound.ProcessedUtc` / `IntegrationInbound.ProcessedUtc`: set when the queue consumer has completed handling the message.
- `DispatchTarget`: stores the queue name used for dispatch, or `ignored` if no queue rule matched.