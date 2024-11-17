param(
    [parameter(Mandatory = $false)]
    [string]$tag = "latest",

    [parameter(Mandatory = $false)]
    [string]$repo = "localhost:5001",

    [parameter(Mandatory = $false)]
    [switch]$withRegoPolicy,

    [parameter(Mandatory = $false)]
    [switch]$skipPush,

    [parameter(Mandatory = $false)]
    [string]$digestFileDir = "",

    [parameter(Mandatory = $false)]
    [string[]]
    [ValidateSet(
        "blobfuse-launcher",
        "ccr-attestation",
        "ccr-governance",
        "ccr-init",
        "ccr-secrets",
        "ccr-proxy",
        "ccr-proxy-ext-processor",
        "ccr-client-proxy",
        "code-launcher",
        "identity",
        "otel-collector",
        "local-skr")]
    $containers
)
. $PSScriptRoot/../helpers.ps1

$root = git rev-parse --show-toplevel
$buildRoot = "$root/build"

$ccrContainers = @(
    "blobfuse-launcher",
    "ccr-attestation",
    "ccr-governance",
    "ccr-init",
    "ccr-secrets",
    "ccr-proxy",
    "ccr-proxy-ext-processor",
    "ccr-client-proxy",
    "code-launcher",
    "identity",
    "otel-collector",
    "local-skr",
    "skr"
)

if ($digestFileDir -eq "") {
    $digestFileDir = [IO.Path]::GetTempPath()
}

$push = $skipPush -eq $false
$skipRegoPolicy = $withRegoPolicy -eq $false

$index = 0
foreach ($container in $ccrContainers) {
    $index++
    if ($null -eq $containers -or $containers.Contains($container)) {
        Write-Host -ForegroundColor DarkGreen "Building $container container ($index/$($ccrContainers.Count))"
        pwsh $buildRoot/ccr/build-$container.ps1 -tag $tag -repo $repo -push:$push
        CheckLastExitCode
    }
    else {
        Write-Host -ForegroundColor DarkBlue "Skipping building $container container ($index/$($ccrContainers.Count))"
    }

    docker image ls $repo/${container}:$tag | grep $container 1>$null
    if ($LASTEXITCODE -ne 0) {
        throw "$container image not found. Must build image for it to get included in the sidecar-digests file."
    }

    if ($env:GITHUB_ACTIONS -eq "true") {
        # remove the local image after pushing to free up disk space on the runner machine.
        Write-Host -ForegroundColor DarkGreen "Removing $repo/${container}:$tag image to make space"
        $image = docker image ls $repo/${container}:$tag --format='{{json .}}' | ConvertFrom-Json
        docker image rm $image.ID --force # Remove by ID so that all tagged references get removed.
    }

    Write-Host -ForegroundColor DarkGray "================================================================="
}

pwsh $buildRoot/build-ccr-digests.ps1 `
    -repo $repo `
    -tag $tag `
    -outDir $digestFileDir `
    -push:$push `
    -skipRegoPolicy:$skipRegoPolicy

