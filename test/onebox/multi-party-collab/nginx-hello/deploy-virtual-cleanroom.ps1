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

#https://learn.microsoft.com/en-us/powershell/scripting/learn/experimental-features?view=powershell-7.4#psnativecommanderroractionpreference
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

$root = git rev-parse --show-toplevel
# Create or update configmap which gets projected into the ccr-governance container via a volume.
kubectl create configmap insecure-virtual `
    --from-file=ccr_gov_pub_key=$root/samples/governance/insecure-virtual/keys/ccr_gov_pub_key.pem `
    --from-file=ccr_gov_priv_key=$root/samples/governance/insecure-virtual/keys/ccr_gov_priv_key.pem `
    --from-file=attestation_report=$root/samples/governance/insecure-virtual/attestation/attestation-report.json `
    -o yaml `
    --dry-run=client | `
    kubectl apply -f -

& {
    # Disable $PSNativeCommandUseErrorActionPreference for this scriptblock
    $PSNativeCommandUseErrorActionPreference = $false
    kubectl delete pod virtual-cleanroom --force
}

kubectl apply -f $outDir/deployments/virtual-cleanroom-pod.yaml
if (!$nowait) {
    # Use below to access the nginx container directly on http://localhost:8181 w/o going thru ccr-proxy/sidecar.
    # Get-Job -Command "*kubectl port-forward virtual-cleanroom*" | Stop-Job
    # Get-Job -Command "*kubectl port-forward virtual-cleanroom*" | Remove-Job
    # kubectl port-forward virtual-cleanroom 8181:8080 &
    # curl http://localhost:8181

    # Wait for the pod to be ready before fetching its IP.
    kubectl wait --for=condition=ready pod -l app=virtual-cleanroom --timeout=180s
    # https://dustinspecker.com/posts/resolving-kubernetes-services-from-host-when-using-kind/
    $podIP = kubectl get pod virtual-cleanroom -o jsonpath="{.status.podIP}"
    Write-Host "Pod IP address: $podIP"

    # wait for nginx-hello endpoint to be up.
    $timeout = New-TimeSpan -Minutes 5
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    while ((docker exec cleanroom-control-plane curl -o "''" -w "'%{http_code}'" -s -k https://${podIP}:8080) -ne "'200'") {
        Write-Host "Waiting for nginx-hello endpoint to be up at https://${podIP}:8080"
        Start-Sleep -Seconds 3
        if ($stopwatch.elapsed -gt $timeout) {
            throw "Hit timeout waiting for nginx-hello endpoint to be up."
        }
    }

    # The network policy only allows GET "/" path. Anything else is blocked.
    # below request should pass
    docker exec cleanroom-control-plane curl -s -k https://${podIP}:8080

    # below should fail
    $expectedResponse = '{"code":"RequestNotAllowed","message":"Failed ccr policy check: Requested API is not allowed"}'
    $response = docker exec cleanroom-control-plane curl -s -k https://${podIP}:8080/blah
    if ($response -ne $expectedResponse) {
        Write-Host -ForegroundColor Red "Did not get expected response. Received: $response."
        exit 1
    }

    $response = docker exec cleanroom-control-plane curl -X POST -s -k https://${podIP}:8080
    if ($response -ne $expectedResponse) {
        Write-Host -ForegroundColor Red "Did not get expected response. Received: $response."
        exit 1
    }

    $base64CaCert = $(cat $outDir/cleanroomca.crt | base64 -w 0)
    # Run the same scenarios via the ccr-client-proxy that then avoids insecure "curl -k" method used above.
    @"
apiVersion: v1
kind: Pod
metadata:
  name: ccr-client-proxy
  labels:
    app: ccr-client-proxy
spec:
  containers:
  - name: ccr-client-proxy
    image: localhost:5001/ccr-client-proxy:latest
    command:
    - /bin/bash
    - -c
    - /home/envoy/bootstrap.sh
    env:
    - name: CA_CERT
      value: $base64CaCert
  restartPolicy: Never
  hostAliases:
  - ip: "$podIP"
    hostnames:
    - "ccr.cleanroom.local"
"@ > $outDir/deployments/client-proxy-pod.yaml

    & {
        # Disable $PSNativeCommandUseErrorActionPreference for this scriptblock
        $PSNativeCommandUseErrorActionPreference = $false
        kubectl delete pod ccr-client-proxy --force
    }

    kubectl apply -f $outDir/deployments/client-proxy-pod.yaml
    kubectl wait --for=condition=ready pod -l app=ccr-client-proxy --timeout=180s

    Get-Job -Command "*kubectl port-forward ccr-client-proxy*" | Stop-Job
    Get-Job -Command "*kubectl port-forward ccr-client-proxy*" | Remove-Job
    kubectl port-forward ccr-client-proxy 10081:10080 &

    # Need to wait a bit for the port-forward to start.
    bash $root/src/scripts/wait-for-it.sh --timeout=20 --strict 127.0.0.1:10081 -- echo "ccr-client-proxy is available"

    curl -s http://ccr.cleanroom.local:8080 --proxy http://127.0.0.1:10081

    # below should fail
    $expectedResponse = '{"code":"RequestNotAllowed","message":"Failed ccr policy check: Requested API is not allowed"}'
    $response = curl -s http://ccr.cleanroom.local:8080/blah --proxy http://127.0.0.1:10081
    if ($response -ne $expectedResponse) {
        Write-Host -ForegroundColor Red "Did not get expected response. Received: $response."
        exit 1
    }
}

if (!$skiplogs) {
    # TODO (gsinha): Add logic to download logs
    # mkdir -p $outDir/results
}