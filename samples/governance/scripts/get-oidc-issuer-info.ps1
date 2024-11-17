Function Get-Oidc-Issuer-Info {
  [CmdletBinding()]
  param
  (
    [string]
    $port = ""
  )

  . $PSScriptRoot/common.ps1

  $port = GetPortOrDie($port)

  curl -sS -X GET localhost:$port/oidc/issuerInfo | jq
}