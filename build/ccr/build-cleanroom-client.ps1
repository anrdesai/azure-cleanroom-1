param(
    [parameter(Mandatory = $false)]
    [string]$tag = "latest",

    [parameter(Mandatory = $false)]
    [string]$repo = "docker.io",

    [parameter(Mandatory = $false)]
    [switch]$push
)

if ($repo) {
    $imageName = "$repo/cleanroom-client:$tag"
}
else {
    $imageName = "cleanroom-client:$tag"
}

. $PSScriptRoot/../helpers.ps1

$root = git rev-parse --show-toplevel
$buildRoot = "$root/build"

docker image build `
    --output="$root/src/tools/cleanroom-client/dist" `
    --target=dist `
    -f $buildRoot/docker/Dockerfile.azcliext.cleanroom "$buildRoot/.."
CheckLastExitCode

docker image build `
    -t $imageName `
    -f $buildRoot/docker/Dockerfile.cleanroom-client "$root/src/tools/cleanroom-client"
CheckLastExitCode

# Extract the open-api spec.
docker image build `
    --output="$root/src/tools/cleanroom-client/app/schema" `
    --target=openapi-dist `
    -f $buildRoot/docker/Dockerfile.cleanroom-client "$root/src/tools/cleanroom-client"
CheckLastExitCode

if ($push) {
    docker push $imageName
    CheckLastExitCode
}