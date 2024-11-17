param(
    [parameter(Mandatory = $false)]
    [string]$tag = "latest",

    [parameter(Mandatory = $false)]
    [string]$repo = "docker.io",

    [parameter(Mandatory = $false)]
    [switch]$push
)
. $PSScriptRoot/../helpers.ps1

$root = git rev-parse --show-toplevel
$buildRoot = "$root/build"

pwsh $buildroot/build-ccf-network-security-policy.ps1 -tag $tag -repo $repo -push:$push
pwsh $buildroot/build-ccf-recovery-service-security-policy.ps1 -tag $tag -repo $repo -push:$push