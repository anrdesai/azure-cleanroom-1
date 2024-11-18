# Local (insecure) Secure Key Release (SKR) tooling

The tool instantiates a web server (http://localhost:port) which mimics the same REST API `key/release` endpoint as the [Secure Key Release](https://github.com/microsoft/confidential-sidecar-containers/blob/main/cmd/skr/README.md#secure-key-release-skr) container to replicate the entire key release process on non-SEV-SNP environments like local docker compose setups. This is an **insecure implementation** that is meant for development consumption only and **not for production environments**. It uses the allow all CCE policy, a fixed private key and an attestation report extracted from a UVM to clone a secure UVM environment. It useful for local development and testing wher other containers can release secrets from Azure Key Vault service via the `key/release` POST method and need not run in a SEV-SNP CACI setup.

## Try it out
- Create a key in KV Premium/mHSM.
    ```powershell
    az login --use-device-code
    ./insecure-sample/create-key.ps1 -keyName "testkey" [-mhsmName "<mhsm-name>" | -vaultName "<vault-name>"]
    ```
- Build and start `local-skr` container.
    ```powershell
    $root = git rev-parse --show-toplevel
    pwsh $root/build/ccr/build-local-skr.ps1
    docker run -d -p 8284:8284 --name local-skr local-skr
    ```
- Release the key via `local-skr` container.
    ```powershell
    ./insecure-sample/release-key.ps1 -keyName "testkey" [-mhsmName "<mhsm-name>" | -vaultName "<vault-name>"]
    ```
## Setup
When creating a key in Key Vault mHSM/Premium that should be released via `local-skr`, the key release policy configured in KV must be the following:
```json
{
  "version": "1.0.0",
  "anyOf": [
    {
      "authority": "https://sharedneu.neu.attest.azure.net",
      "allOf": [
        {
          "claim": "x-ms-sevsnpvm-hostdata",
          "equals": "73973b78d70cc68353426de188db5dfc57e5b766e399935fb73a61127ea26d20"
        },
        {
          "claim": "x-ms-compliance-status",
          "equals": "azure-compliant-uvm"
        },
        {
          "claim": "x-ms-attestation-type",
          "equals": "sevsnpvm"
        }
      ]
    }
  ]
}
```
Any other value for `authority` or `x-ms-sevsnpvm-hostdata` is not supported.

## HTTP API

The `key/release` POST method expects a JSON of the following format:
```json
{
  "maa_endpoint": "<maa endpoint>",
  "akv_endpoint": "<akv endpoint>",
  "kid": "<key identifier>",
  "access_token": "aad token as the command will run in a resource without managed identity support"
}
```

Upon success, the `key/release` POST method response carries a `StatusOK` header and a payload of the following format:
```json
{
  "key": "<key in JSON Web Key format>"
}
```

Upon error, the `key/release` POST method response carries a `StatusForbidden` header and a payload of the following format:
```json
{
  "error": "<error message>"
}
```