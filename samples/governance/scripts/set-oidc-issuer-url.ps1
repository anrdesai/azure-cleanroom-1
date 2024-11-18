function Set-Oidc-IssuerUrl {
    [CmdletBinding()]
    param
    (
        [string]
        [Parameter(Mandatory)]
        $url,

        [string]
        $port = ""
    )

    . $PSScriptRoot/common.ps1

    $port = GetPortOrDie($port)

    $body = @"
{
  "url": "$url"
}
"@
    curl -sS -X POST localhost:$port/oidc/setIssuerUrl -k -H "Content-Type: application/json" -d $body
}
