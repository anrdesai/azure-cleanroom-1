param(
    [parameter(Mandatory = $false)]
    [string]$tag = "latest",

    [parameter(Mandatory = $false)]
    [string]$repo = "docker.io",

    [parameter(Mandatory = $false)]
    [switch]$push
)

. $PSScriptRoot/../helpers.ps1

if ($repo) {
    $imageName = "$repo/ccr-proxy-ext-processor:$tag"
}
else {
    $imageName = "ccr-proxy-ext-processor:$tag"
}

$root = git rev-parse --show-toplevel
docker image build -t $imageName `
    -f $PSScriptRoot/../docker/Dockerfile.proxy-ext-processor $root
CheckLastExitCode
if ($push) {
    docker push $imageName
    CheckLastExitCode
}
