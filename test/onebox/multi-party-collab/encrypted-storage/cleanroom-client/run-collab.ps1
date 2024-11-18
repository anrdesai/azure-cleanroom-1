[CmdletBinding()]
param
(
    [switch]
    $NoBuild,

    [ValidateSet('mcr', 'local')]
    [string]$registry = "local"
)

$outDir = "$PSScriptRoot/generated"
rm -rf $outDir
Write-Host "Using $registry registry for cleanroom container images."
$root = git rev-parse --show-toplevel
$datastoreOutdir = "$PSScriptRoot/../../generated/datastores"
mkdir -p "$datastoreOutdir"
$samplePath = "$root/test/onebox/multi-party-collab"

pwsh $root/src/tools/cleanroom-client/deploy-cleanroom-client.ps1 `
    -outDir $outDir `
    -datastoreOutDir $datastoreOutdir `
    -dataDir $samplePath

if ($LASTEXITCODE -gt 0) {
    Write-Host -ForegroundColor Red "deploy-cleanroom-client returned non-zero exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

pwsh $root/test/onebox/multi-party-collab/deploy-virtual-cleanroom-governance.ps1 `
    -NoBuild:$NoBuild `
    -registry $registry `
    -projectName "ob-consumer-client" `
    -initialMemberName "consumer"
az cleanroom governance client remove --name "ob-publisher-client"

$cleanroomClientEndpoint = "localhost:8321"
pwsh $PSScriptRoot/run-scenario-generate-template-policy.ps1 `
    -registry $registry  `
    -cleanroomClientEndpoint $cleanroomClientEndpoint `
    -datastoreOutDir $datastoreOutdir

if ($LASTEXITCODE -gt 0) {
    Write-Host -ForegroundColor Red "run-scenario-generate-template-policy returned non-zero exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

pwsh $PSScriptRoot/../convert-template.ps1 -outDir $outDir

pwsh $PSScriptRoot/../deploy-virtual-cleanroom.ps1 -outDir $outDir -skipLogs

curl --fail-with-body `
    -w "\n%{method} %{url} completed with %{response_code}\n" `
    -X POST $cleanroomClientEndpoint/datastore/download -H "content-type: application/json" -d @"
{
    "name" : "consumer-output",
    "configName": "$datastoreOutdir/encrypted-storage-cleanroom-client-consumer-datastore-config",
    "targetFolder": "$outDir/results"
}
"@
curl --fail-with-body `
    -w "\n%{method} %{url} completed with %{response_code}\n" `
    -X POST $cleanroomClientEndpoint/logs/download -H "content-type: application/json" -d @"
{
    "configName": "$outDir/configurations/publisher-config",
    "targetFolder": "$outDir/results",
    "datastoreConfigName": "$datastoreOutdir/encrypted-storage-cleanroom-client-publisher-datastore-config"
}
"@
curl --fail-with-body `
    -w "\n%{method} %{url} completed with %{response_code}\n" `
    -X POST $cleanroomClientEndpoint/telemetry/download -H "content-type: application/json" -d @"
{
    "configName": "$outDir/configurations/publisher-config",
    "targetFolder": "$outDir/results",
    "datastoreConfigName": "$datastoreOutdir/encrypted-storage-cleanroom-client-publisher-datastore-config"
}
"@

Write-Host "Application logs:"
cat $outDir/results/application-telemetry*/**/demo-app.log

# Check that expected output files got created.
$expectedFiles = @(
    "$PSScriptRoot/generated/results/consumer-output/**/output.gz",
    "$PSScriptRoot/generated/results/application-telemetry*/**/demo-app.log",
    "$PSScriptRoot/generated/results/infrastructure-telemetry*/**/application-telemetry*-blobfuse.log",
    "$PSScriptRoot/generated/results/infrastructure-telemetry*/**/application-telemetry*-blobfuse-launcher.log",
    "$PSScriptRoot/generated/results/infrastructure-telemetry*/**/application-telemetry*-blobfuse-launcher.traces",
    "$PSScriptRoot/generated/results/infrastructure-telemetry*/**/demo-app-code-launcher.log",
    "$PSScriptRoot/generated/results/infrastructure-telemetry*/**/consumer-output*-blobfuse.log",
    "$PSScriptRoot/generated/results/infrastructure-telemetry*/**/consumer-output*-blobfuse-launcher.log",
    "$PSScriptRoot/generated/results/infrastructure-telemetry*/**/consumer-output*-blobfuse-launcher.traces",
    "$PSScriptRoot/generated/results/infrastructure-telemetry*/**/infrastructure-telemetry*-blobfuse.log",
    "$PSScriptRoot/generated/results/infrastructure-telemetry*/**/infrastructure-telemetry*-blobfuse-launcher.log",
    "$PSScriptRoot/generated/results/infrastructure-telemetry*/**/infrastructure-telemetry*-blobfuse-launcher.traces",
    "$PSScriptRoot/generated/results/infrastructure-telemetry*/**/publisher-input*-blobfuse.log",
    "$PSScriptRoot/generated/results/infrastructure-telemetry*/**/publisher-input*-blobfuse-launcher.log",
    "$PSScriptRoot/generated/results/infrastructure-telemetry*/**/publisher-input*-blobfuse-launcher.traces"
)

$missingFiles = @()
foreach ($file in $expectedFiles) {
    if (!(Test-Path $file)) {
        $missingFiles += $file
    }
}

if ($missingFiles.Count -gt 0) {
    Write-Host -ForegroundColor Red "Did not find the following expected file(s). Check clean room logs for any failure(s):"
    foreach ($file in $missingFiles) {
        Write-Host -ForegroundColor Red $file
    }
    
    exit 1
}