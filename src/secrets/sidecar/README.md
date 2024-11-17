# ccr-secrets

The `ccr-secrets` container instantiates a web server (<http://localhost:9300>) which exposes a REST API so that other containers can use it to unwrap secrets.

## HTTP API
### Secrets
The `/secrets/unwrap` POST method expects a request body in the following format:
```json
{
  "clientId": "<...>",
  "tenantId": "<...>",
  "kid": "<password-kid>",
  "akvEndpoint": "https://<kid-vault>.vault.azure.net",
  "kek":
  {
    "kid": "<kek-kid>",
    "akvEndpoint": "https://<kek-kid-vault>.vault.azure.net",
    "maaEndpoint": "https://sharedneu.neu.attest.azure.net"
  }
}
```
| Name | Description |
| --- | --- |
| `clientId` | The client Id of the managed identity that should be used to access the `kid` secret.
| `tenantId` | The tenant Id for the client Id.|
| `kid` | The name of the wrapped secret in key vault.|
| `akvEndpoint` | The key vault endpoint for the `kid` secret.|
| `kek.kid` | The name of the key in key vault that was used to wrap the `kid` secret.|
| `kek.akvEndpoint` | The key vault endpoint for the `kek.kid` secret.|
| `kek.maaEndpoint` | The MAA endpoint that has the SKR policy configured for `kek.kid` key release.|

It responds with a JSON of the following format:
```json
{
  "value": "<base64encoded-secretvalue>"
}
```
| Name | Description |
| --- | --- |
| `value` | Base64 encoded unwrapped secert value for the `kid` secret.|

## Configuration
The following environment variables control the configuration of this sidecar:
| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `SKR_PORT` | `string` | No | The port on which the SKR sidecar is listening on. Defaults to 8284.|
| `IDENTITY_PORT` | `string` | No | The port on which the identity sidecar is listening on. Defaults to 8290.|
| `ASPNETCORE_URLS` | `string` | No | The port on which the secrets sidecar listens on. Defaults to 9300.|

To override the port the secrets sidecar listens on set the `ASPNETCORE_URLS` environment variable 
for the secrets sidecar container with value `http://+:<PORT>`. Eg `http://+:9458`.
