[CmdletBinding()]
param
(
    [string]
    $outDir = "$PSScriptRoot/generated"
)

$root = git rev-parse --show-toplevel
docker image ls aci-to-k8s | grep aci-to-k8s 1>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "aci-to-k8s image not found. Building image."
    pwsh $root/build/onebox/build-aci-to-k8s.ps1
}

Write-Host "Converting template"
docker run --rm `
    -v ${outDir}/deployments:/workspace `
    -w /workspace `
    -u $(id -u $env:USER) `
    aci-to-k8s `
    --template-file ./cleanroom-arm-template.json `
    --out-dir .
