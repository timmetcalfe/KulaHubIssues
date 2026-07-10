# API and database sync options

Describes options available when wanting to synchronise database entries with internal or external systems

### Database triggers

Database triggers can be used in cases such as a new or updated form or contact needs to be replicated to another client. It uses a rule table to determine which clients should have data replicated and to where.

In the Database SQL Project, see the FormMirrorRules.sql for details of the rules table and FormTrigger.sql for the trigger SQL.

As of the 10/07/2026, a trigger is set up on the Contacts table.

### Change Event Stream in Azure Database

When a record is inserted, updated or deleted, an event is sent to an Event Hub. It can be set on specific tables. A good option but care must be taken when replicating data in the same database because the replicated data will also generate an event. Less of a need for this when using an API through which all database operations are routed because that can then use the outbox pattern. Using an API also allows to set the correlation id across all request.

### API, IntegrationInbox and IntegrationDispatch

Route all data through the API which will use a transaction when saving the entity and saving the IntegrationInbox. A separate Function app then periodically checks the IntegrationInbox for rows not processed and checks if there is a routing rule for it. If there is, it creates an entry in IntegrationDispatch and marks the IntegrationInbox row as processed. A Function app periodically checks the IntegrationDispatch entry and sends a message to the associated queue and marks the IntegrationDispatch entry as processed.

[DrawIO diagram showing workflow - signin to tim@metsoft.co.uk to view it](https://app.diagrams.net/#Wb!B5nMMt8psEiIPn04_NeXQEU73rat8LlCpaMiXDneV5PdAOsRxmrySaJT5kcfCpFZ%2F01QRXJNKRBRJBVCL5JZ5DINO2MYK2K2A65#%7B%22pageId%22%3A%22E8MRzWS5IuExZnuHcwu4%22%7D)