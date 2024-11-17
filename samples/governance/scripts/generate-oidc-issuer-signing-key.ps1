function Generate-Oidc-Issuer-Signing-Key {
    [CmdletBinding()]
    param
    (
        [string]
        $port = ""
    )

    . $PSScriptRoot/common.ps1

    $port = GetPortOrDie($port)

    curl -sS -X POST localhost:$port/oidc/generateSigningKey -k -H "Content-Type: application/json" | jq
}
