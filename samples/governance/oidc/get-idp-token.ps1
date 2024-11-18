[CmdletBinding()]
param
(
  [string]
  [Parameter(Mandatory)]
  $sub,

  [string]
  [Parameter(Mandatory)]
  $tenantId,

  [string]
  [Parameter(Mandatory)]
  $aud,

  [string]
  $contractId = "",

  [string]
  $govPort = "7990"
)

if ($contractId -eq "")
{
  $contractId = $sub;
}

$client_assertion = $(curl -sS -X POST "http://localhost:$govPort/oauth/token?&sub=${sub}&tenantId=${tenantId}&aud=${aud}" -H "x-ms-ccr-governance-api-path-prefix: app/contracts/${contractId}") | jq -r '.value';
return $client_assertion
