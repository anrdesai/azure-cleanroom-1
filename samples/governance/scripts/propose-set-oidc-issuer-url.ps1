function Propose-Set-Oidc-IssuerUrl {
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

  $response = (curl -sS -X POST -H "content-type: application/json" localhost:$port/proposals/create -d `
      @"
  {
    "actions": [{
       "name": "set_oidc_issuer_url",
       "args": {
        "issuer_url": "$url"
       }
    }]
  }
"@)

  return $response
}