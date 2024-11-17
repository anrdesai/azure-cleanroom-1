function Add-Secret {
    [CmdletBinding()]
    param
    (
        [string]
        [Parameter(Mandatory)]
        $contractId,

        [string]
        [Parameter(Mandatory)]
        $name,

        [string]
        [Parameter(Mandatory)]
        $value,

        [string]
        $port = ""
    )

    . $PSScriptRoot/common.ps1

    $port = GetPortOrDie($port)

    $body = @"
{
  "value": "$value"
}
"@
    $response = (curl -sS -X PUT localhost:$port/contracts/$contractId/secrets/$name -k -H "Content-Type: application/json" -d $body)
    return $response
}
