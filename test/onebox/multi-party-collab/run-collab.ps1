[CmdletBinding()]
param
(
    [switch]
    $NoBuild,

    [ValidateSet('mcr', 'local')]
    [string]$registry = "mcr"
)

Write-Host "Using $registry registry for cleanroom container images."
$root = git rev-parse --show-toplevel
pwsh $root/test/onebox/multi-party-collab/deploy-virtual-cleanroom-governance.ps1 `
    -NoBuild:$NoBuild `
    -registry $registry
pwsh $root/test/onebox/multi-party-collab/run-scenario-generate-template-policy.ps1 -registry $registry
pwsh $root/test/onebox/multi-party-collab/convert-template.ps1
pwsh $root/test/onebox/multi-party-collab/deploy-virtual-cleanroom.ps1

# Check that expected output files got created.
$expectedFiles = @(
    "$root/test/onebox/multi-party-collab/generated/results/consumer-output/output.gz",
    "$root/test/onebox/multi-party-collab/generated/results/application-telemetry/demo-app.log",
    "$root/test/onebox/multi-party-collab/generated/results/infrastructure-telemetry/application-telemetry-blobfuse.log",
    "$root/test/onebox/multi-party-collab/generated/results/infrastructure-telemetry/code_launcher.log",
    "$root/test/onebox/multi-party-collab/generated/results/infrastructure-telemetry/consumer-output-blobfuse.log",
    "$root/test/onebox/multi-party-collab/generated/results/infrastructure-telemetry/infrastructure-telemetry-blobfuse.log",
    "$root/test/onebox/multi-party-collab/generated/results/infrastructure-telemetry/publisher-input-blobfuse.log"
)

$missingFiles = @()
foreach ($file in $expectedFiles) {
    if (!(Test-Path $file)) {
        $missingFiles += $file
    }
}

if ($missingFiles.Count -gt 0) {
    Write-Host -ForegroundColor Red "Did not find the following expected file(s). Check clean room logs for any failure(s):"
    foreach ($file in $expectedFiles) {
        Write-Host -ForegroundColor Red $file
    }
    
    exit 1
}