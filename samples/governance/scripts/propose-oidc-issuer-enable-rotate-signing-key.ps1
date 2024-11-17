function Propose-Oidc-Issuer-Enable-Rotate-Signing-Key {
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
       "name": "oidc_issuer_enable_rotate_signing_key",
       "args": {}
    }]
  }
"@)

  return $response
}