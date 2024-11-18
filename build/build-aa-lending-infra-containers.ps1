param(
    [parameter(Mandatory = $false)]
    [string]$tag = "latest",

    [parameter(Mandatory = $false)]
    [string]$repo = "docker.io",

    [parameter(Mandatory = $false)]
    [switch]$push
)

. $PSScriptRoot/helpers.ps1

$ccrContainers = @{
    "ccr-init"                = ''
    "ccr-proxy"               = ''
    "ccr-proxy-ext-processor" = ''
    "crypto-sidecar"          = ''
    "inmemory-keyprovider"    = ''
}

$root = git rev-parse --show-toplevel
$buildRoot = "$root/build"
foreach ($container in $ccrContainers.Keys) {
    Write-Host -ForegroundColor DarkGreen "Building $container container"
    pwsh $buildroot/ccr/build-$container.ps1 $($ccrContainers[$container] -eq "" ? $null : $ccrContainers[$container]) -tag $tag -repo $repo -push:$push
    CheckLastExitCode
    Write-Host -ForegroundColor DarkGray "================================================================="
}