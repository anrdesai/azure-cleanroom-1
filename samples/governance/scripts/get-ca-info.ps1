Function Get-CA-Info {
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

    curl -sS -X GET localhost:$port/contracts/$contractId/ca/info | jq
}