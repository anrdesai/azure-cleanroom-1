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
    $imageName = "$repo/blobfuse-launcher:$tag"
}
else {
    $imageName = "blobfuse-launcher:$tag"
}

$root = git rev-parse --show-toplevel
$external = Join-Path $root -ChildPath "/external"
# To avoid pulling the latest blobfuse commit commenting the below line.
# git submodule update --init --recursive $external/azure-storage-fuse

docker image build -t $imageName -f $PSScriptRoot/../docker/Dockerfile.blobfuse-launcher $root
CheckLastExitCode
if ($push) {
    docker push $imageName
    CheckLastExitCode
}