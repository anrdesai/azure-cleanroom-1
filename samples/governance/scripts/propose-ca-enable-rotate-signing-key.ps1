function Propose-CA-Enable-Rotate-Signing-Key {
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
       "name": "ca_enable_rotate_signing_key",
       "args": {"contractId": "$contractId"}
    }]
  }
"@)

    return $response
}