param(
    [parameter(Mandatory = $true)]
    [string]$tag,

    [parameter(Mandatory = $true)]
    [string]$repo,

    [string]$outDir = "",

    [switch]$push,

    [switch]$skipRegoPolicy
)
. $PSScriptRoot/helpers.ps1

if ($outDir -eq "") {
    $outDir = "."
}

$ccrContainers = @(
    "blobfuse-launcher",
    "ccr-attestation",
    "ccr-governance",
    "ccr-init",
    "ccr-secrets",
    "ccr-proxy",
    "ccr-proxy-ext-processor",
    "code-launcher",
    "identity",
    "otel-collector",
    "skr"
)

$digests = @()
$containerPolicies = @()
foreach ($container in $ccrContainers) {
    $digest = Get-Digest -repo $repo -containerName $container -tag $tag
    $digests += [ordered]@{
        image                = $container
        digest               = "$digest"
        policyDocument       = $container + "-policy"
        policyDocumentDigest = ""
    }

    if (!$skipRegoPolicy) {
        $containerRegoPolicy = Get-Container-Rego-Policy -repo $repo -containerName $container -digest $digest -outDir $outDir
        $containerDebugRegoPolicy = Get-Container-Rego-Policy -repo $repo -containerName $container -digest $digest -outDir $outDir -debugMode
    }
    else {
        $containerRegoPolicy = "{}"
    }

    $templateJson = Get-Content -Path "$PSScriptRoot/templates/$container.json" | ConvertFrom-Json
    $policyJson = Get-Content -Path "$PSScriptRoot/templates/$container-policy.json" | ConvertFrom-Json
    $containerPolicies += [ordered]@{
        image        = $container
        templateJson = $templateJson
        policy       = @{
            json       = $policyJson
            rego       = $containerRegoPolicy
            rego_debug = $containerDebugRegoPolicy
        }
    }
}

foreach ($containerPolicy in $containerPolicies) {
    $imageName = $containerPolicy["image"]
    $fileName = $imageName + "-policy.yaml"
    $containerPolicy | ConvertTo-Yaml | Out-File $outDir/$fileName
    if ($push) {
        Set-Location $outDir
        oras push "$repo/policies/$imageName-policy:$tag" ./$fileName
        CheckLastExitCode
        $policyDocumentDigest = Get-Digest -repo "$repo/policies" -containerName $imageName-policy -tag $tag
        foreach ($digest in $digests) {
            if ($digest["image"] -eq $imageName) {
                $digest["policyDocumentDigest"] = "$policyDocumentDigest"
                break
            }
        }
    }
}

$digests | ConvertTo-Yaml | Out-File $outDir/sidecar-digests.yaml

if ($push) {
    Set-Location $outDir
    oras push "$repo/sidecar-digests:$tag" ./sidecar-digests.yaml
    CheckLastExitCode
}