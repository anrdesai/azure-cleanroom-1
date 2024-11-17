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
    $imageName = "$repo/ccr-secrets:$tag"
}
else {
    $imageName = "ccr-secrets:$tag"
}

$root = git rev-parse --show-toplevel
docker image build -t $imageName -f $PSScriptRoot/../docker/Dockerfile.ccr-secrets $root
CheckLastExitCode

if ($push) {
    docker push $imageName
    CheckLastExitCode
}