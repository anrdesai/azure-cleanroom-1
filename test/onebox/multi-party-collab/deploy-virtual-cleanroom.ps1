[CmdletBinding()]
param
(
    [string]
    $outDir = "$PSScriptRoot/generated",

    [switch]
    $nowait,

    [switch]
    $skiplogs
)

$root = git rev-parse --show-toplevel

# Create or update configmap which gets projected into the ccr-governance container via a volume.
kubectl create configmap insecure-virtual `
    --from-file=ccr_gov_pub_key=$root/samples/governance/insecure-virtual/keys/ccr_gov_pub_key.pem `
    --from-file=ccr_gov_priv_key=$root/samples/governance/insecure-virtual/keys/ccr_gov_priv_key.pem `
    --from-file=attestation_report=$root/samples/governance/insecure-virtual/attestation/attestation-report.json `
    -o yaml `
    --dry-run=client | `
    kubectl apply -f -

kubectl delete pod virtual-cleanroom --force
kubectl apply -f $outDir/deployments/virtual-cleanroom-pod.yaml
if (!$nowait) {
    pwsh $PSScriptRoot/wait-for-virtual-cleanroom.ps1
}

if (!$skiplogs) {
    mkdir -p $root/test/onebox/multi-party-collab/generated/results
    az cleanroom datasink download `
        --cleanroom-config $root/test/onebox/multi-party-collab/generated/configurations/consumer-config `
        --name consumer-output `
        --target-folder $root/test/onebox/multi-party-collab/generated/results

    az cleanroom telemetry download `
        --cleanroom-config $root/test/onebox/multi-party-collab/generated/configurations/publisher-config `
        --target-folder $root/test/onebox/multi-party-collab/generated/results

    az cleanroom logs download `
        --cleanroom-config $root/test/onebox/multi-party-collab/generated/configurations/publisher-config `
        --target-folder $root/test/onebox/multi-party-collab/generated/results

    Write-Host "Application logs:"
    cat $root/test/onebox/multi-party-collab/generated/results/application-telemetry/demo-app.log
}