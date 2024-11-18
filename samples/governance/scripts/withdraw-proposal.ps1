function Withdraw-Proposal {
  [CmdletBinding()]
  param
  (
    [string]
    [Parameter(Mandatory)]
    $proposalId,

    [string]
    $port = ""
  )

  . $PSScriptRoot/common.ps1

  $port = GetPortOrDie($port)

  curl -sS -X POST localhost:$port/proposals/$proposalId/withdraw | jq
}