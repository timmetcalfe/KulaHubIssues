# DualWrite_APISync
Work related to the issues KulaHub is experiencing

## API integration metadata

External API callers can now send an optional `sourceSystemKey` alongside `originType` on create contact, add note, and create form requests.

- `originType` indicates whether the change originated externally or internally.
- `sourceSystemKey` identifies the external partner when known, for example `Dealer` or `Polaris`.
- Use `sourceSystemKey` when you need routing or loop-prevention logic to distinguish one external source from another.

Example request body:

```json
{
	"firstName": "Taylor",
	"lastName": "Partner11",
	"email": "taylor.partner11@example.com",
	"sourceSystemKey": "Dealer",
	"originType": "ExternalClient"
}
```
