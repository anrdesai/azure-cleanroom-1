param(
    [parameter(Mandatory = $false)]
    [string]$tag = "latest",

    [parameter(Mandatory = $false)]
    [string]$repo = "docker.io",

    [parameter(Mandatory = $false)]
    [switch]$push
)
. $PSScriptRoot/helpers.ps1

$ccrContainers = @(
    "blobfuse2",
    "testclient",
    "testvolmount",
    "testnetcon"
)

$root = git rev-parse --show-toplevel
$buildRoot = "$root/build"
foreach ($container in $ccrContainers) {
    Write-Host -ForegroundColor DarkGreen "Building $container container"
    pwsh $buildroot/test/build-$container.ps1 -tag $tag -repo $repo -push:$push
    CheckLastExitCode
    Write-Host -ForegroundColor DarkGray "================================================================="
}