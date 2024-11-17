function Activate-Current-Member {
  [CmdletBinding()]
  param
  (
    [string]
    $port = ""
  )

  . $PSScriptRoot/common.ps1

  $port = GetPortOrDie($port)

  Write-Host "Member status:"
  curl -sS -X GET localhost:$port/members | jq
  CheckLastExitCode

  Write-Host "Activating member..."
  curl -sS -X POST localhost:$port/members/statedigests/ack
  CheckLastExitCode
  curl -sS -X GET localhost:$port/members | jq
  CheckLastExitCode
}