This is a .NET 10 application and Azure SQL database to demonstrate reliably receiving API calls from external clients, call their APIs when they need updated information or do something to the data after it has been received. It uses an event driven architecture design with an outbox pattern and Azure Service Bus queues and is hosted in Azure. The system is a sample multi-tenant basic CRM with entities such as clients, organisations, contacts, forms and notes.

Use Entity Framework Core for data access. The data model can be found in docs/data-model.md

The system is comprised of the following projects:

## Web API
Purpose: For clients to call the CRM system to do things like create a new contact or add a note to an existing contact or get a list of contacts. When an entity is created, updated or deleted in the database from the Web API, it should take place within a transaction that also creates a new row in the IntegrationInbox table.

## Web application
Purpose: A .NET Razor Page application with a simple deisng to allow creating a contact, viewing a contact and any related forms and notes and adding forms and notes to a contact, creating a form. It should also handle swapping between each client to see the contacts, forms, notes in each client.

When an entity is created, updated or deleted in the database from the web application, it should take place within a transaction that also creates a new row in the IntegrationInbox table.

## Azure Function app with timer trigger for processing IntegrationInbox events in the IntegrationInbox table.
Purpose: This will process rows in the IntegrationInbox table that have not been processed and when processed mark them as complete. There are three possible outcomes for each row:
1. It is not an event the system is interested in, for example a ClientId that does not require further processing and should just be marked as completed.
2. An event that does require processing and needs to make an outbound call, should be placed in the IntegrationOutbound table as part of a transaction that also marks the event as being processsed.
3. An event that does require processing and needs to do something like data procecssing on the related item in the database, should be placed in the IntegrationInbound table as part of a transaction that also marks the event as being processsed.

## Azure Function app with timer trigger for processing IntegrationOutbound events in the IntegrationOutbound table.
Purpose: To process rows in the IntegrationOutbound table that have not been dispatched and send them directly to the relevant Azure Service Bus queue based on configured processing rules. For example, if ClientId = 4 then send to a queue called clientid4-outbound.

## Azure Function app with timer trigger for processing IntegrationInbound events in the IntegrationInbound table.
Purpose: To process rows in the IntegrationInbound table that have not been dispatched and send them to the relevant Azure Service Bus queue.

## Azure Function app to read from a queue for inbound events for a client called Southbridge Retail
Purpose: To read messages from a queue for inbound events and once processed, mark the corresponding row in IntegrationInbound table as processed.

## Azure Function app to read from a queue for outbound events for a client called Polaris Advisory
Purpose: To read messages from a queue for outbound events and once processed, mark the corresponding row in IntegrationOutbound table as processed.
