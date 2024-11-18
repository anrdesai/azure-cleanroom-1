function Get-Event {
    [CmdletBinding()]
    param
    (
        [string]
        [Parameter(Mandatory)]
        $contractId,

        [string]
        [Parameter()]
        $id = "",

        [string]
        [Parameter()]
        $scope = "",

        [switch]
        $all,

        [string]
        $port = ""
    )

    . $PSScriptRoot/common.ps1

    $port = GetPortOrDie($port)

    $url = "localhost:$port/contracts/$contractId/events"
    $query = ""
    if ($id -ne "") {
        $query += "&id=$id"
    }

    if ($scope -ne "") {
        $query += "&scope=$scope"
    }

    if ($all) {
        $query += "&from_seqno=1"
    }

    if ($query -ne "") {
        $url += "?$query"
    }

    $response = (curl -sS -X GET $url)
    return $response
}