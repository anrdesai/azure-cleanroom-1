[CmdletBinding()]
param
(
    [Parameter(Mandatory)]
    [string]$resourceGroup,

    [string]
    $location = "westus",

    [string]
    $outDir = "$PSScriptRoot/generated",

    [switch]
    $nowait,

    [switch]
    $skiplogs
)

$root = git rev-parse --show-toplevel

function Get-UniqueString ([string]$id, $length = 13) {
    $hashArray = (new-object System.Security.Cryptography.SHA512Managed).ComputeHash($id.ToCharArray())
    -join ($hashArray[1..$length] | ForEach-Object { [char]($_ % 26 + [byte][char]'a') })
}

$uniqueString = Get-UniqueString("${resourceGroup}")
$cleanRoomName = "${uniqueString}-cl"
Write-Host "Deploying clean room $cleanRoomName in resource group $resourceGroup"
az deployment group create `
    --resource-group $resourceGroup `
    --name $cleanRoomName `
    --template-file "$outDir/deployments/cleanroom-arm-template.json" `
    --parameters location=$location

if (!$nowait) {
    $ccrIP = az container show `
        --name $cleanRoomName `
        -g $resourceGroup `
        --query "ipAddress.ip" `
        --output tsv
    Write-Host "Clean Room IP address: $ccrIP"

    $caCert = "$outDir/cleanroomca.crt"
    $base64CaCert = $(cat $caCert | base64 -w 0)

    # Start the ccr-client-proxy that listens on 10080 and handles HTTPS connection to the cleanroom endpoint.
    # --add-host parameter is used below so that subject alt. name wildcard entry of *.cleanroom.local
    # in the server cert that the clean room presents passes SSL verification perfomed by envoy.
    # The image_client must use "ccr.cleanroom.local" so that the hostname verification with the server cert works.
    docker stop ccr-client-proxy 1>$null 2>$null
    docker rm ccr-client-proxy 1>$null 2>$null
    docker run `
        --name ccr-client-proxy `
        -d `
        --add-host=ccr.cleanroom.local:${ccrIP} `
        -p 10080:10080 `
        -e CA_CERT=$base64CaCert `
        ccr-client-proxy `
        /bin/bash -c ./bootstrap.sh

    # Need to wait a bit for the proxy to start.
    bash $root/src/scripts/wait-for-it.sh --timeout=20 --strict 127.0.0.1:10080 -- echo "ccr-client-proxy is available"
    Start-Sleep -Seconds 5

    curl -s --fail-with-body http://ccr.cleanroom.local:8080 --proxy http://127.0.0.1:10080
    if ($LASTEXITCODE -gt 0) {
        Write-Host -ForegroundColor Red "GET / call failed as curl returned non-zero exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }

    # below should fail
    $expectedResponse = '{"code":"RequestNotAllowed","message":"Failed ccr policy check: Requested API is not allowed"}'
    $response = curl -s http://ccr.cleanroom.local:8080/blah --proxy http://127.0.0.1:10080
    if ($response -ne $expectedResponse) {
        Write-Host -ForegroundColor Red "Did not get expected response. Received: $response."
        exit 1
    }

    $response = curl -X POST -s http://ccr.cleanroom.local:8080 --proxy http://127.0.0.1:10080
    if ($response -ne $expectedResponse) {
        Write-Host -ForegroundColor Red "Did not get expected response. Received: $response."
        exit 1
    }

    # The same set of above requests should fail when hitting the ccrIP directly w/o the client proxy.
    curl -s -k --fail-with-body https://${ccrIP}:8080
    if ($LASTEXITCODE -gt 0) {
        Write-Host -ForegroundColor Red "GET / call failed as curl returned non-zero exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }

    # below should fail
    $expectedResponse = '{"code":"RequestNotAllowed","message":"Failed ccr policy check: Requested API is not allowed"}'
    $response = curl -s -k https://${ccrIP}:8080/blah
    if ($response -ne $expectedResponse) {
        Write-Host -ForegroundColor Red "Did not get expected response. Received: $response."
        exit 1
    }

    $response = curl -X POST -s -k https://${ccrIP}:8080
    if ($response -ne $expectedResponse) {
        Write-Host -ForegroundColor Red "Did not get expected response. Received: $response."
        exit 1
    }
}

if (!$skiplogs) {
    # TODO (gsinha): Add logic to download logs
    # mkdir -p $outDir/results
}