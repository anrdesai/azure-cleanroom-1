function Vote-Proposal {
  [CmdletBinding()]
  param
  (
    [string]
    [Parameter(Mandatory)]
    $proposalId,

    [string]
    [Parameter(Mandatory)]
    [ValidateSet('accept', 'reject')]
    $vote,

    [string]
    $port = "",

    [hashtable]
    $headers = @{"Content-Type" = "application/json" }
  )

  . $PSScriptRoot/common.ps1

  $port = GetPortOrDie($port)

  if ($vote -eq "accept") {
    $response = Invoke-WebRequest -Method POST -Uri "http://localhost:$port/proposals/$proposalId/ballots/vote_accept" -Headers $headers
    $response.Content | jq
  }
  else {
    $response = Invoke-WebRequest -Method POST -Uri "http://localhost:$port/proposals/$proposalId/ballots/vote_reject" -Headers $headers
    $response.Content | jq
  }
}