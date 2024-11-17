function Generate-CA-Signing-Key {
    [CmdletBinding()]
    param
    (
        [string]
        $contractId,

        [string]
        $port = ""
    )

    . $PSScriptRoot/common.ps1

    $port = GetPortOrDie($port)

    curl -sS -X POST localhost:$port/contracts/$contractId/ca/generateSigningKey -k -H "Content-Type: application/json" | jq
}
