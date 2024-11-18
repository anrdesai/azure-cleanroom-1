[CmdletBinding()]
param
(
    [string]
    $outDir = "$PSScriptRoot/generated",

    [string]
    $datastoreOutdir = "$PSScriptRoot/../generated/datastores",

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
    pwsh $root/test/onebox/multi-party-collab/wait-for-virtual-cleanroom.ps1 `
        -containerName depa-training-code-launcher
}

if (!$skiplogs) {
    mkdir -p $outDir/results
    az cleanroom datastore download `
        --config $datastoreOutdir/ml-training-consumer-datastore-config `
        --name output `
        --dst $outDir/results

    az cleanroom telemetry download `
        --cleanroom-config $outDir/configurations/tdp-config `
        --datastore-config $datastoreOutdir/ml-training-publisher-datastore-config `
        --target-folder $outDir/results

    az cleanroom logs download `
        --cleanroom-config $outDir/configurations/tdp-config `
        --datastore-config $datastoreOutdir/ml-training-publisher-datastore-config `
        --target-folder $outDir/results

    Write-Host "Application logs:"
    cat $outDir/results/application-telemetry*/**/depa-training.log
}