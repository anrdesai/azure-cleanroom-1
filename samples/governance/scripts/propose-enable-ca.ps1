function Propose-Enable-CA {
    [CmdletBinding()]
    param
    (
        [string]
        $contractId,

        [string]
        $port = ""
    )

    . $PSScriptRoot/common.ps1

    $port = GetPortOrDie($port)

    $response = (curl -sS -X POST -H "content-type: application/json" localhost:$port/proposals/create -d `
            @"
  {
    "actions": [{
       "name": "enable_ca",
       "args": {"contractId": "$contractId"}
    }]
  }
"@)

    return $response
}