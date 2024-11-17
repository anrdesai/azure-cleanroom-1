function Propose-Enable-Oidc-Issuer {
    [CmdletBinding()]
    param
    (
        [string]
        $port = ""
    )

    . $PSScriptRoot/common.ps1

    $port = GetPortOrDie($port)

    $response = (curl -sS -X POST -H "content-type: application/json" localhost:$port/proposals/create -d `
            @"
  {
    "actions": [{
       "name": "enable_oidc_issuer",
       "args": {"kid": "$((New-Guid).ToString("N"))"}
    }]
  }
"@)

    return $response
}