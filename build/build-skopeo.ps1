param(
)

. $PSScriptRoot/helpers.ps1

docker image build `
    --output=$PSScriptRoot/bin --target skopeo-rootfs `
    -f $PSScriptRoot/docker/Dockerfile.skopeo `
    "$PSScriptRoot/.."

CheckLastExitCode