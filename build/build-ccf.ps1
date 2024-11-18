param(
    [switch]
    $acme
)

. $PSScriptRoot/helpers.ps1

if ($acme) {
    $root = git rev-parse --show-toplevel
    $sandbox_common = "$root/samples/governance/acme-tls/sandbox_common"
    mkdir -p $sandbox_common
    bash $root/samples/governance/keygenerator.sh --name member0 --gen-enc-key -o $sandbox_common

    $dockerfile = "$PSScriptRoot/docker/Dockerfile.ccf_app_js.virtual-acme"
    $tag = "latest"
    docker build -t "ccf-acme:$tag" -f $dockerfile "$PSScriptRoot/.."
    CheckLastExitCode
}
else {
    $dockerfile = "$PSScriptRoot/docker/Dockerfile.ccf_app_js.virtual"
    $tag = "js-virtual"
    docker build -t "ccf:$tag" -f $dockerfile "$PSScriptRoot/../src/governance/ccf-app"
    CheckLastExitCode
}